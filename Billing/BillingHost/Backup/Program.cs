// POP3 Email Client Test Harness (Main)
// =====================================
//
// copyright by Peter Huber, Singapore, 2006
// this code is provided as is, bugs are probable, free for any use at own risk, no 
// responsibility accepted. All rights, title and interest in and to the accompanying content retained.  :-)
//
// based on POP3 Client as a C# Class, by Bill Dean, http://www.codeproject.com/csharp/Pop3client.asp 
// based on Retrieve Mail From a POP3 Server Using C#, by Agus Kurniawan, http://www.codeproject.com/csharp/popapp.asp 
// based on Post Office Protocol - Version 3, http://www.ietf.org/rfc/rfc1939.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Text;

namespace Pop3 {
  class Program {
    static void Main(string[] args) {
      // TODO: Remove the comment signs from the next line, if you want to create some sample emails.
//      SendTestmail();
      //If you get a run time error her: SmtpFailedRecipientException, 'Mailbox unavailable. The server response was: 5.7.1 Unable to relay for',
      //then you need to change the settings of the local IIS/SMTP server.

      Console.WriteLine("POP3 MIME Client Demo");
      Console.WriteLine("=====================");
      Console.WriteLine();

      //prepare pop client
      // TODO: Replace server address, username and password with your own credentials.
      Pop3.Pop3MimeClient DemoClient = new Pop3.Pop3MimeClient("pop.gmail.com", 995, true, "Username@gmail.com", "password");

      DemoClient.Trace += new TraceHandler(Console.WriteLine);
      DemoClient.ReadTimeout = 60000; //give pop server 60 seconds to answer

      //establish connection
      DemoClient.Connect();

      //get mailbox stats
      int numberOfMailsInMailbox, mailboxSize;
      DemoClient.GetMailboxStats(out numberOfMailsInMailbox, out mailboxSize);

      //get at most the xx first emails
      RxMailMessage mm;
      int downloadNumberOfEmails;
      int maxDownloadEmails = 99;
      if (numberOfMailsInMailbox<maxDownloadEmails) {
        downloadNumberOfEmails = numberOfMailsInMailbox;
      } else {
        downloadNumberOfEmails = maxDownloadEmails;
      }
      for (int i = 1;i <= downloadNumberOfEmails;i++) {
        DemoClient.GetEmail(i, out mm);
        if (mm==null) {
          Console.WriteLine("Email " + i.ToString() + " cannot be displayed.");
        } else {
          Console.WriteLine(mm.MailStructure());
        }
      }

      ////uncomment the following code if you want to write the raw text of the emails to a file.
      //string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
      //string Email;
      //DemoClient.IsCollectRawEmail = true;
      //string fileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Emails From POP3 Server.TXT";
      //using (StreamWriter sw = File.CreateText(fileName)) {
      //  for (int i = 1;i <= maxDownloadEmails;i++) {
      //    sw.WriteLine("Email: " + i.ToString() + "\n===============\n\n");
      //    DemoClient.GetRawEmail(i, out Email);
      //    sw.WriteLine(Email);
      //    sw.WriteLine();
      //  }
      //  sw.Close();
      //}

      //close connection
      DemoClient.Disconnect();

      Console.WriteLine();
      Console.WriteLine("======== Press Enter to end program");
      Console.ReadLine();
    }

    static void SendMessage(SmtpClient client, MailMessage message) {
      client.Send(message);
      Console.WriteLine("Message '" + message.Subject + "'sent");
      // Clean up.
      message.Dispose();
    }


    /// <summary>
    /// Produce some test emails.
    /// </summary>
    static void SendTestmail() {
      SmtpClient client = new SmtpClient("localhost");

      //pure text email
      // TODO: Replace email addresses.
      MailAddress from = new MailAddress("FromName at server.com", "Any name or remove this string");
      MailAddress to = new MailAddress("ToName at server.com", "Any name or remove this string");
      MailMessage message;
      AlternateView thisAlternateView1;
      AlternateView thisAlternateView2;

      //just body text email
      message = new MailMessage(from, to);
      message.Subject = "Text only email";
      message.Body = "This is a test e-mail message with just a plain text body.";
      SendMessage(client, message);

      //pure text email with body and 1 alternative view
      message = new MailMessage(from, to);
      message.Subject = "pure text email with body and 1 alternative view";
      message.Body = "This is the body text";
      thisAlternateView1 = AlternateView.CreateAlternateViewFromString("This is the alternative view text");
      message.AlternateViews.Add(thisAlternateView1);
      SendMessage(client, message);

      //pure text email with no body but 1 alternative view
      message = new MailMessage(from, to);
      message.Subject = "pure text email with no body but 1 alternative view";
      thisAlternateView1 = AlternateView.CreateAlternateViewFromString("This is the alternative view text");
      message.AlternateViews.Add(thisAlternateView1);
      SendMessage(client, message);

      //pure text email with plain text body and 2 alternative views
      message = new MailMessage(from, to);
      message.Subject = "pure text email with plain text body and 2 alternative views";
      message.Body = "This is the body text";
      thisAlternateView1 = AlternateView.CreateAlternateViewFromString("This is the alternative view text");
      message.AlternateViews.Add(thisAlternateView1);
      thisAlternateView2 = AlternateView.CreateAlternateViewFromString("<html><body>This is a <b>HTML<b> text body</body></html>");
      thisAlternateView2.ContentType.MediaType = "text/html";
      message.AlternateViews.Add(thisAlternateView2);
      SendMessage(client, message);

      //email with attachment and UTF8 body encoding
      message = new MailMessage(from, to);
      message.Subject = "email with attachment and UTF8 body encoding";
      message.SubjectEncoding = System.Text.Encoding.ASCII;
      message.Body = "Body text";
      // Include some non-ASCII characters in body and subject.
      string someArrows = new string(new char[] { '\u2190', '\u2191', '\u2192', '\u2193' });
      message.Body += Environment.NewLine + someArrows;
      message.BodyEncoding =  System.Text.Encoding.UTF8;
      // TODO: Write path and name of an existing file into the following string
      Attachment thisAttachment = new Attachment(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Pic.GIF");
      message.Attachments.Add(thisAttachment);

      message.BodyEncoding =  System.Text.Encoding.UTF8;
      message.Subject = "test message 1" + someArrows;
      message.SubjectEncoding = System.Text.Encoding.UTF7;

      client.Send(message);

      Console.WriteLine("Sending emails completed. Give POP3 server a bit of time to receive the emails, then press 'Return' key.");
      Console.ReadLine();
    }
  }
}
