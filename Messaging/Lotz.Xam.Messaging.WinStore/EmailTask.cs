using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;

namespace Lotz.Xam.Messaging
{
    internal class EmailTask : IEmailTask
    {
        public EmailTask()
        {
        }

        #region IEmailTask Members

        public bool CanSendEmail
        {
            get { return true; }
        }

        private IEmailMessage Email;

        public void SendEmail(IEmailMessage email)
        {
            if (email == null)
                throw new ArgumentNullException("email");

            if (CanSendEmail)
            {
                if (((EmailMessage) email).UseDataTransferManager)
                {
                    Email = email;
                    //DataTransferManager implementation
                    DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
                    dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.ShareTextHandler);
                    DataTransferManager.ShowShareUI();
                }
                else
                {
                    // NOTE: Refer to http://www.faqs.org/rfcs/rfc2368.html for info on mailto protocol

                    if (email.RecipientsBcc.Count > 0)
                        Debug.WriteLine("Bcc headers are inherently unsafe to include in a message generated from a URL - ignoring RecipientBcc");

                    var sb = new StringBuilder();

                    sb.AppendFormat(CultureInfo.InvariantCulture, "mailto:{0}?", ToDelimitedAddress(email.Recipients));

                    if (email.RecipientsCc.Count > 0)
                        sb.AppendFormat(CultureInfo.InvariantCulture, "cc={0}&", ToDelimitedAddress(email.RecipientsCc));

                    sb.AppendFormat(CultureInfo.InvariantCulture, "subject={0}&body={1}",
                        email.Subject, email.Message);

                    var escaped = Uri.EscapeUriString(sb.ToString());

                    Launcher.LaunchUriAsync(new Uri(escaped, UriKind.Absolute));
                }
            }
        }

        private void ShareTextHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;
            if (Email != null)
	        {
                request.Data.Properties.Title = Email.Subject;
                var body = HtmlFormatHelper.CreateHtmlFormat(Email.Message);
                request.Data.SetHtmlFormat(body);
            }
        }

        public void SendEmail(string to, string subject, string message)
        {
            SendEmail(new EmailMessage(to, subject, message));
        }

        #endregion

        #region Methods

        private static string ToDelimitedAddress(ICollection<string> collection)
        {
            return collection.Count == 0 ? string.Empty : string.Join(",", collection);
        }

        #endregion

    }
}