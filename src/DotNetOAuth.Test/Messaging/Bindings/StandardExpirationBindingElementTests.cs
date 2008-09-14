﻿//-----------------------------------------------------------------------
// <copyright file="StandardExpirationBindingElementTests.cs" company="Andrew Arnott">
//     Copyright (c) Andrew Arnott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOAuth.Test.Messaging.Bindings {
	using System;
	using DotNetOAuth.Messaging;
	using DotNetOAuth.Messaging.Bindings;
	using DotNetOAuth.Test.Mocks;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class StandardExpirationBindingElementTests : MessagingTestBase {
		[TestMethod]
		public void SendSetsTimestamp() {
			TestExpiringMessage message = new TestExpiringMessage(MessageTransport.Indirect);
			message.Recipient = new Uri("http://localtest");
			((IExpiringProtocolMessage)message).UtcCreationDate = DateTime.Parse("1/1/1990");

			Channel channel = CreateChannel(MessageProtection.Expiration, MessageProtection.Expiration);
			channel.Send(message);
			Assert.IsTrue(DateTime.UtcNow - ((IExpiringProtocolMessage)message).UtcCreationDate < TimeSpan.FromSeconds(3), "The timestamp on the message was not set on send.");
		}

		[TestMethod]
		public void VerifyGoodTimestampIsAccepted() {
			this.Channel = CreateChannel(MessageProtection.Expiration, MessageProtection.Expiration);
			this.ParameterizedReceiveProtectedTest(DateTime.UtcNow, false);
		}

		[TestMethod, ExpectedException(typeof(ExpiredMessageException))]
		public void VerifyBadTimestampIsRejected() {
			this.Channel = CreateChannel(MessageProtection.Expiration, MessageProtection.Expiration);
			this.ParameterizedReceiveProtectedTest(DateTime.UtcNow - StandardExpirationBindingElement.DefaultMaximumMessageAge - TimeSpan.FromSeconds(1), false);
		}
	}
}