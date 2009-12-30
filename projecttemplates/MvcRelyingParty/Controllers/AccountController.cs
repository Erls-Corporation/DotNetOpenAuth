﻿namespace MvcRelyingParty.Controllers {
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Security.Principal;
	using System.Web;
	using System.Web.Mvc;
	using System.Web.Security;
	using System.Web.UI;
	using DotNetOpenAuth.Messaging;
	using DotNetOpenAuth.OpenId;
	using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
	using DotNetOpenAuth.OpenId.RelyingParty;
	using RelyingPartyLogic;

	[HandleError]
	public class AccountController : Controller {
		/// <summary>
		/// Initializes a new instance of the <see cref="AccountController"/> class.
		/// </summary>
		/// <remarks>
		/// This constructor is used by the MVC framework to instantiate the controller using
		/// the default forms authentication and OpenID services.
		/// </remarks>
		public AccountController()
			: this(null, null) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AccountController"/> class.
		/// </summary>
		/// <param name="formsAuth">The forms auth.</param>
		/// <param name="relyingParty">The relying party.</param>
		/// <remarks>
		/// This constructor is not used by the MVC framework but is instead provided for ease
		/// of unit testing this type. 
		/// </remarks>
		public AccountController(IFormsAuthentication formsAuth, IOpenIdRelyingParty relyingParty) {
			this.FormsAuth = formsAuth ?? new FormsAuthenticationService();
			this.RelyingParty = relyingParty ?? new OpenIdRelyingPartyService();
		}

		/// <summary>
		/// Gets the forms authentication module to use.
		/// </summary>
		public IFormsAuthentication FormsAuth { get; private set; }

		/// <summary>
		/// Gets the OpenID relying party to use for logging users in.
		/// </summary>
		public IOpenIdRelyingParty RelyingParty { get; private set; }

		/// <summary>
		/// Prepares a web page to help the user supply his login information.
		/// </summary>
		/// <returns>The action result.</returns>
		public ActionResult LogOn() {
			return View();
		}

		/// <summary>
		/// Accepts the login information provided by the user and redirects
		/// the user to their Provider to complete authentication.
		/// </summary>
		/// <param name="openid_identifier">The user-supplied identifier.</param>
		/// <param name="rememberMe">Whether the user wants a persistent cookie.</param>
		/// <param name="returnUrl">The URL to direct the user to after successfully authenticating.</param>
		/// <returns>The action result.</returns>
		[AcceptVerbs(HttpVerbs.Post), ValidateAntiForgeryToken]
		public ActionResult LogOn(string openid_identifier, bool rememberMe, string returnUrl) {
			Identifier userSuppliedIdentifier;
			if (Identifier.TryParse(openid_identifier, out userSuppliedIdentifier)) {
				try {
					var request = this.RelyingParty.CreateRequest(openid_identifier, Realm.AutoDetect, Url.ActionFull("LogOnReturnTo"));
					request.SetUntrustedCallbackArgument("rememberMe", rememberMe ? "1" : "0");

					// This might be signed so the OP can't send the user to a dangerous URL.
					// Of course, if that itself was a danger then the site is vulnerable to XSRF attacks anyway.
					if (!string.IsNullOrEmpty(returnUrl)) {
						request.SetUntrustedCallbackArgument("returnUrl", returnUrl);
					}

					// Ask for the user's email, not because we necessarily need it to do our work,
					// but so we can display something meaningful to the user as their "username"
					// when they log in with a PPID from Google, for example.
					request.AddExtension(new ClaimsRequest {
						Email = DemandLevel.Require,
						FullName = DemandLevel.Request,
						PolicyUrl = Url.ActionFull("PrivacyPolicy", "Home"),
					});

					return request.RedirectingResponse.AsActionResult();
				} catch (ProtocolException ex) {
					ModelState.AddModelError("OpenID", ex.Message);
				}
			} else {
				ModelState.AddModelError("OpenID", "This doesn't look like a valid OpenID.");
			}

			return View();
		}

		/// <summary>
		/// Handles the positive assertion that comes from Providers.
		/// </summary>
		/// <returns>The action result.</returns>
		/// <remarks>
		/// This method instructs ASP.NET MVC to <i>not</i> validate input
		/// because some OpenID positive assertions messages otherwise look like
		/// hack attempts and result in errors when validation is turned on.
		/// </remarks>
		[AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post), ValidateInput(false)]
		public ActionResult LogOnReturnTo() {
			var response = this.RelyingParty.GetResponse();
			if (response != null) {
				switch (response.Status) {
					case AuthenticationStatus.Authenticated:
						var token = RelyingPartyLogic.User.ProcessUserLogin(response);
						bool rememberMe = response.GetUntrustedCallbackArgument("rememberMe") == "1";
						this.FormsAuth.SignIn(token.ClaimedIdentifier, rememberMe);
						string returnUrl = response.GetCallbackArgument("returnUrl");
						if (!String.IsNullOrEmpty(returnUrl)) {
							return Redirect(returnUrl);
						} else {
							return RedirectToAction("Index", "Home");
						}
					case AuthenticationStatus.Canceled:
						ModelState.AddModelError("OpenID", "It looks like you canceled login at your OpenID Provider.");
						break;
					case AuthenticationStatus.Failed:
						ModelState.AddModelError("OpenID", response.Exception.Message);
						break;
				}
			}

			// If we're to this point, login didn't complete successfully.
			// Show the LogOn view again to show the user any errors and
			// give another chance to complete login.
			return View("LogOn");
		}

		/// <summary>
		/// Logs the user out of the site and redirects the browser to our home page.
		/// </summary>
		/// <returns>The action result.</returns>
		public ActionResult LogOff() {
			this.FormsAuth.SignOut();
			return RedirectToAction("Index", "Home");
		}
	}
}
