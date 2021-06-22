<%@ page language="C#" autoeventwireup="true" codebehind="WebForm1.aspx.cs" inherits="CountlySampleAspNet.WebForm1" async="true" %>

<script runat="server">

   private void sample_1(object sender, EventArgs e)
   {
      //Debug.WriteLine("Initializing Countly");
      string str = eventName.Value;
      sample_output.InnerHtml = str.ToUpper();
   }
</script>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <h3>Countly SDK ASP.NET sample </h3>

    <form runat="server">
        <input runat="server" id="eventName" type="text" />
        <input runat="server" id="button1" type="submit" value="Enter..." onserverclick="sample_1" />

        <hr />
        <h3>Results: </h3>
        <span runat="server" id="sample_output" />
    </form>
</body>
</html>
