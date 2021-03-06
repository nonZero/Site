﻿using System.Data.Common;
using System.IO;
using System.Text.RegularExpressions;
using GeoAPI.Geometries;
using IsraelHiking.Common;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nest;
using NSubstitute;

namespace IsraelHiking.DataAccess.Tests
{
    [TestClass]
    public class WikimediaCommonGatewayTests
    {
        [TestMethod]
        public void GetImageUrl()
        {
            var options = new NonPublicConfigurationData();
            var optionsContainer = Substitute.For<IOptions<NonPublicConfigurationData>>();
            var logger = Substitute.For<ILogger>();
            optionsContainer.Value.Returns(options);
            var gateway = new WikimediaCommonGateway(optionsContainer, logger);
            var results = gateway.GetImageUrl("File:Israel_Hiking_Map_עין_מחוללים.jpeg").Result;

            Assert.IsNotNull(results);
        }
    }
}
