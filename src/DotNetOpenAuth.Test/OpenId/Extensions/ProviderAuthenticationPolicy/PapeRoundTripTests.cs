﻿//-----------------------------------------------------------------------
// <copyright file="PapeRoundTripTests.cs" company="Outercurve Foundation">
//     Copyright (c) Outercurve Foundation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.Test.OpenId.Extensions.ProviderAuthenticationPolicy {
	using System;
	using DotNetOpenAuth.OpenId;
	using DotNetOpenAuth.OpenId.Extensions.ProviderAuthenticationPolicy;
	using DotNetOpenAuth.Test.OpenId.Extensions;
	using NUnit.Framework;

	[TestFixture]
	public class PapeRoundTripTests : OpenIdTestBase {
		[Test]
		public void Trivial() {
			var request = new PolicyRequest();
			var response = new PolicyResponse();
			ExtensionTestUtilities.Roundtrip(Protocol.Default, new[] { request }, new[] { response });
		}

		[Test]
		public void Full() {
			var request = new PolicyRequest();
			request.MaximumAuthenticationAge = TimeSpan.FromMinutes(10);
			request.PreferredAuthLevelTypes.Add(Constants.AssuranceLevels.NistTypeUri);
			request.PreferredAuthLevelTypes.Add("customAuthLevel");
			request.PreferredPolicies.Add(AuthenticationPolicies.MultiFactor);
			request.PreferredPolicies.Add(AuthenticationPolicies.PhishingResistant);

			var response = new PolicyResponse();
			response.ActualPolicies.Add(AuthenticationPolicies.MultiFactor);
			response.ActualPolicies.Add(AuthenticationPolicies.PhishingResistant);
			response.AuthenticationTimeUtc = DateTime.UtcNow - TimeSpan.FromMinutes(5);
			response.AssuranceLevels[Constants.AssuranceLevels.NistTypeUri] = "1";
			response.AssuranceLevels["customlevel"] = "ABC";
			response.NistAssuranceLevel = NistAssuranceLevel.Level2;

			ExtensionTestUtilities.Roundtrip(Protocol.Default, new[] { request }, new[] { response });
		}
	}
}
