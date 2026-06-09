using BenchmarkDotNet.Running;
using FastData.Benchmarks;

var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
