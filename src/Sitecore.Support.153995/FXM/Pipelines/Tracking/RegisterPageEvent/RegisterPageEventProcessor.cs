namespace Sitecore.Support.FXM.Pipelines.Tracking.RegisterPageEvent
{
  using Analytics.Tracking;
  using Sitecore;
  using Sitecore.Analytics;
  using Sitecore.Analytics.Data;
  using Sitecore.Analytics.Data.Items;
  using Sitecore.Analytics.Outcome.Model;
  using Sitecore.Data;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.FXM.Tracking;
  using Sitecore.FXM.Utilities;
  using System;
  using System.Collections.Generic;
  using Sitecore.FXM.Pipelines.Tracking.RegisterPageEvent;

  public class RegisterPageEventProcessor : IRegisterPageEventProcessor, IRegisterPageEventProcessor<RegisterPageEventArgs>
  {
    public void Process(RegisterPageEventArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      Assert.IsNotNull(args.PageEventItem, "No item has been found corresponding to the page event.");
      Assert.IsNotNull(args.CurrentPage, "The current page is not tracked in the current session.  No events can be triggered.");
      Assert.IsNotNull(args.CurrentPage.Session, "There is no Analytics Session for the curent page. No events can be triggered.");
      Assert.IsNotNull(args.CurrentPage.Session.Interaction, "There is no Interaction for the curent page. No events can be triggered.");
      Assert.IsNotNull(args.CurrentPage.Session.Contact, "The current contact isn't available. No events can be triggered.");

      int valueBeforeRegister = args.CurrentPage.Session.Interaction.Value;
      switch (args.EventParameters.EventType)
      {
        case PageEventType.Goal:
        case PageEventType.Event:
          this.TriggerPageEvent(args);
          break;

        case PageEventType.Campaign:
          this.TriggerCampaign(args);
          break;

        case PageEventType.Outcome:
          this.TriggerOutcome(args);
          break;

        case PageEventType.Element:
          this.TriggerElement(args);
          break;
      }
      int valueAfterRegister = args.CurrentPage.Session.Interaction.Value;
      if (valueAfterRegister > valueBeforeRegister)
      {
        args.CurrentPage.Session.Contact.System.Value += valueAfterRegister - valueBeforeRegister;
      }
    }

    private string ToQueryString(IDictionary<string, string> dictionary)
    {
      string str = string.Empty;
      if (dictionary != null)
      {
        foreach (KeyValuePair<string, string> pair in dictionary)
        {
          if (str.Length > 0)
          {
            str = str + "&";
          }
          str = string.Concat(new object[] { str, pair.Key, '=', pair.Value });
        }
      }
      return str;
    }

    protected virtual void TriggerCampaign(RegisterPageEventArgs args)
    {
      CampaignItem campaignItem = new CampaignItem(args.PageEventItem);
      args.CurrentPage.TriggerCampaign(campaignItem);
    }

    protected virtual void TriggerElement(RegisterPageEventArgs args)
    {
      Field innerField = args.PageEventItem.Fields["__Tracking"];
      TrackingField field2 = new TrackingField(innerField);
      foreach (TrackingField.PageEventData data in field2.Events)
      {
        Item item = field2.InnerField.Item;
        Sitecore.Analytics.Data.PageEventData pageData = new Sitecore.Analytics.Data.PageEventData(data.Name)
        {
          Data = data.Data,
          ItemId = item.ID.Guid,
          DataKey = StringUtil.Right(item.Paths.Path, 100)
        };        
        args.CurrentPage.Register(pageData);
      }
      foreach (CampaignItem item2 in field2.Campaigns)
      {
        args.CurrentPage.TriggerCampaign(item2);
      }
      if (args.EventParameters.Id != default(ID))
      {
        OutcomeUtility.TriggerItemOutcomes(args.PageEventItem);
      }
    }

    protected virtual void TriggerOutcome(RegisterPageEventArgs args)
    {
      if (Tracker.Current != null)
      {
        ID newID = ID.NewID;
        ID id2 = ID.Parse(Tracker.Current.Interaction.InteractionId);
        ID contactId = ID.Parse(Tracker.Current.Contact.ContactId);
        Item pageEventItem = args.PageEventItem;
        ContactOutcome outcome = new ContactOutcome(newID, pageEventItem.ID, contactId)
        {
          DateTime = DateTime.UtcNow.Date,
          InteractionId = id2,
          MonetaryValue = args.EventParameters.MonetaryValue
        };
        foreach (KeyValuePair<string, string> pair in args.EventParameters.Extras)
        {
          outcome.CustomValues[pair.Key] = pair.Value;
        }
        OutcomeUtility.TriggerOutcome(outcome);
      }
    }

    protected virtual void TriggerPageEvent(RegisterPageEventArgs args)
    {
      PageEventItem item = new PageEventItem(args.PageEventItem);
      Sitecore.Analytics.Data.PageEventData pageData = new Sitecore.Analytics.Data.PageEventData(item.Name, item.ID.Guid)
      {
        Data = args.EventParameters.Data,
        DataKey = args.EventParameters.DataKey,
        Text = this.ToQueryString(args.EventParameters.Extras)
      };      
      args.CurrentPage.Register(pageData);      
    }
  }
}
