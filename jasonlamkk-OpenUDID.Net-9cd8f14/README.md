<h4>Who is behind OpenUDID.NET</h4>

<p>The project OpenUDID for iOS was initiated by Yaan Lechelle on 8/28/11 and 
this OpenUDID.Net is the CSharp.Net and SilverLight port created by Jason Lam (co-founder <a href="http://wavespread.com/mobile-apps/openudid-net.html">WaveSpread Technology Limited</a>) on 15/4/2012</p>

<h4>About OpenUDID.JS</h4>

<p>OpenUDID.JS is JavaScript OpenUDID client side script created by Jason Lam with flash and JavaScript on 19/4/2012. 

It demonstrate the possibility of generating a single cross-browser + cross-domain unique idendifier per machine,
which link up all browsering activities for every web user very useful for centralized behavioral analysis.

<p>moved to <a href="https://github.com/jasonlamkk/OpenUDID.Web">OpenUDID.Web</a> as it is only a by-product of this project, the architecture and coding is not very relevant</p>
</p>

<hr/>
<ul>
<li><a href="https://github.com/jasonlamkk">https://github.com/jasonlamkk</a></li>
</ul><h4>Master Branches &amp; Contributors</h4>

<ul>
<li>Windows Phone 7 / Windows Desktop code: <a href="https://github.com/jasonlamkk/OpenUDID.Net">https://github.com/jasonlamkk/OpenUDID.Net</a></li>
<li>iOS / MacOS code: <a href="https://github.com/ylechelle/OpenUDID">https://github.com/ylechelle/OpenUDID</a></li>
<li>Android code (2012 update): <a href="https://github.com/jasonlamkk/OpenUDID">https://github.com/jasonlamkk/OpenUDID</a></li>
<li>Android code: <a href="https://github.com/vieux/OpenUDID">https://github.com/vieux/OpenUDID</a></li>
</ul>


<h4>Usage On Windows Desktop (support .Net framework 2.0 or above)</h4>

<pre><code>

using OpenUDIDCSharp;
String openUDID = OpenUDID.value;

</code></pre>

<h4>Usage On Windows Phone 7</h4>

<pre><code>

using OpenUDIDPhone;
String openUDID = OpenUDID.value;
//
String plainOldDeviceId = OpenUDID.OldDeviceId;// the factory default unique idendifier in base64 format for transition / compatibility purpose
</code></pre>

<h4>Synopsis</h4>

<p>OpenUDID.Net is a created for standarding OpenUDID format across platforms, 
OpenUDID for iOS was initiated by Yaan Lechelle on 8/28/11 to replace the deprecated uniqueIdentifier property of the UIDevice class on iOS (a.k.a. UDID) and otherwise is an industry-friendly equivalent for iOS and Android.</p>

<p>The agenda for this community driven project is to:
- Provide a reliable proxy and replacement for a universal unique device identifier. That is, persistent and sufficiently unique, on a per device basis.
- NOT use an obvious other sensitive unique identifier (like the MAC address) to avoid further deprecation and to protect device-level privacy concerns
- Enable the same OpenUDID to be accessed by any app on the same device
- Supply open-source code to generate and access the OpenUDID, for Windows PC, Windows Phone 7 (,and the .net2.0 version theoretically can port to Windows CE and Windows Mobile 5~6.5 )
- Incorporate, from the beginning, a system that will enable user opt-out for privacy intent</p>

<h4>Context</h4>

<p>If you're not already familiar with UDID's, it's a critical tool for analytic or CRM purposes. A developer could use UDID's as a means to track how much time a user spent in his free app before upgrading to the paid version. UDID's are also helpful for tracking the source of a download when advertising on an ad network. This is a fairly universal need in a thriving ecosystem: developers need the traceability from clicks to downloads to ensure that they pay the right price for their promotion. Proper tracking and funnel conversion is what has made the web a better place, with healthy competition and quantifiable metrics.</p>

<p>In the wake of Apple's decision to deprecate UDID;
 and the absent of UDID in windows desktop platform, some ad networks have already introduced their own proprietary solutions. The main motivation here was to find a UDID replacement not owned by any single provider. It is easy to foresee a fragmented market where UDID management is operated by multiple providers with no cooperation between them. This open source initiative is to enable a better solution for thousands of other mobile app developers.</p>

<h5>Version History</h5>

<ul>
<li>April 2012: launch of the initiative</li>
</ul><h4>Contributions needed</h4>

<p>Implementation / Suggestion of opt-out mechanism, etc...</p>
<p>Equivalent OpenUDID systems on Blackberry, etc...</p>