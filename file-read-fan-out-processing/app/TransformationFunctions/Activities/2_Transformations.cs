using System;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TransformationFunctions.DTOs;

namespace TransformationFunctions.Activities
{
    public static class Transformations
    {
        /// <summary>
        /// Groups the lines into batches. Because line-per-line transformation and persistence
        /// seemed to have too much overhead. Inspired by: https://docs.microsoft.com/en-us/azure/azure-functions/functions-best-practices
        ///
        /// NOTE: Requires environment variable NumberOfLinesInBatch to be set - so be sure to set this through AppSettings
        /// </summary>
        /// <param name="input">Collection of lines read from a file</param>
        /// <param name="log">Logger</param>
        /// <returns>Array of arrays - containing the lines grouped into batches of size NumberOfLinesInBatch</returns>
        [FunctionName("GroupLinesInBatches")]
        public static InputFormat[][] GroupLinesInBatches([ActivityTrigger] InputFormat[] input, ILogger log)
        {
            var batchSize = Convert.ToInt32(Environment.GetEnvironmentVariable("NumberOfLinesInBatch"));

            // Taken from: https://stackoverflow.com/a/30391350/896697 
            var i = 0;
            var result = input.GroupBy(s => i++ / batchSize).Select(g => g.ToArray()).ToArray();
            log.LogInformation($"Converted {input.Length} into {result.Length} batches of {batchSize} lines each");
            log.LogMetric("LinesBatched", input.Length);
            log.LogMetric("BatchesCreated", result.Length);
            return result;
        }

        [FunctionName("ConvertCSVToFormatA")]
        public static FormatAInstance[] ConvertCSVToFormatA([ActivityTrigger] InputFormat[] input, ILogger log)
        {
            var result = new FormatAInstance[input.Length];

            for (var i = 0; i < input.Length; i++)
            {
                var line = input[i];
                var parts = line.S.Split(";");
                if (parts.Length != 4)
                {
                    throw new FormatException(
                        $"Invalid format - cannot convert string {input} to FormatA - expecting semi-colon delimited input string consisting of 4 parts. E.g. 'test13;4803;1835;1558'");
                }

                try
                {
                    var first = Convert.ToInt32(parts[1]);
                    var second = Convert.ToInt32(parts[2]);
                    var third = Convert.ToInt32(parts[3]);
                    result[i] = new FormatAInstance(line)
                        {N = parts[0], F = first, S = second, T = third};
                }
                catch (Exception e)
                {
                    throw new FormatException(
                        $"Invalid format - cannot convert string {input} to FormatA - Numbers cannot be converted to integers. Expecting semi-colon delimited input string consising of 4 parts. E.g. 'test13;4803;1835;1558'",
                        e);
                }
            }
            log.LogMetric("CSVToFormatAConversions", input.Length);
            log.LogInformation($"Converted {input.Length} CSV lines to FormatA");
            return result;
        }

        [FunctionName("ConvertFormatAToFormatB")]
        public static FormatBInstance[] ConvertFormatAToFormatB([ActivityTrigger] FormatAInstance[] input, ILogger log)
        {
            var result = new FormatBInstance[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                var line = input[i];
                result[i] = new FormatBInstance(line);
            }
            log.LogMetric("FormatAToFormatBConversions", input.Length);
            log.LogInformation($"Converted {input.Length} FormatA lines to FormatB");
            return result;
        }
    }
}
