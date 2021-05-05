using System;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransformationFunctions.Activities;
using TransformationFunctions.DTOs;

namespace TransformationLogicTests
{
    [TestClass]
    public class FormatAConversionTests

    {
        [TestMethod]
        public void ConvertMethod_ConvertsCSV_ToFormatA()
        {
            // arrange
            var logFactory = new NullLoggerFactory();
            var input = new InputFormat[1] {new InputFormat("test13;4803;1835;1558")};
                    // act
            var conversion = Transformations.ConvertCSVToFormatA(input, logFactory.CreateLogger("mock"));
            var actual = conversion[0];

            // assert
            Assert.AreEqual("test13", actual.N, "Name property should be equal to first part of CSV line");
            Assert.AreEqual(4803, actual.F, "'First' property should equal 2nd part of CSV line");
            Assert.AreEqual(1835, actual.S, "'Second' property should equal 3rd part of CSV line");
            Assert.AreEqual(1558, actual.T, "'Third' property should equal 4th part of CSV line");
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException), "FormatException expected in case of invalid input")]
        public void ConvertMethod_ThrowsException_InCaseOfInvalidInput()
        {
            // arrange
            var logFactory = new NullLoggerFactory();
            var invalidInput = new InputFormat[1] { new InputFormat("test13;4803;1835;zzz") }; // Note zzzs at the end - should be integers
            var actual = Transformations.ConvertCSVToFormatA(invalidInput, logFactory.CreateLogger("mock"));
        }

        [TestMethod]
        public void ConversionPipelineTest()
        {
            // arrange
            var logFactory = new NullLoggerFactory();
            var input = new InputFormat[1] {new InputFormat("test13;4803;1835;1558")};

            // act
            var formatA = Transformations.ConvertCSVToFormatA(input, logFactory.CreateLogger("mock"));
            var formatB = Transformations.ConvertFormatAToFormatB(formatA, logFactory.CreateLogger("mock"));

            // assert
            Assert.AreEqual("test13", formatA[0].N, "Name property should be equal to first part of CSV line");
        }
    }
}
