# Performance

Benchmark results are produced by the benchmarks project
([`SourceGeneratorFramework.Benchmarks`](../../src/src/SourceGeneratorFramework.Benchmarks)) using
[BenchmarkDotNet](https://benchmarkdotnet.org) and folded here for reference.

## What is measured

All benchmarks measure the **production** code path: generator runs configure
`ValidateCodeWriterScopes = false` and the writer is constructed with `throwOnUnclosedScopes: false`.
Scope tracking is a testing/debug feature and is excluded here because capturing an opening
`StackTrace` per scope dominates both time and allocation (for 1000 small classes it inflates the
writer benchmark from ~1.6 ms/2.3 MB to ~19 ms/22 MB). Tests opt into it so an unclosed `using` or
block fails fast; see [Code-Writer.md](Code-Writer.md#construction-and-scope-validation).

## Environment

- BenchmarkDotNet v0.15.8
- Windows 11 (10.0.28020.2991)
- 13th Gen Intel Core i9-13900KF 3.00GHz, 1 CPU, 32 logical and 24 physical cores
- .NET SDK 10.0.401
- .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
- Toolchain: InProcessEmitToolchain

## CodeWriter

| Method     | Mean     | Error    | StdDev   | Gen0      | Gen1     | Gen2    | Allocated |
| ---------- |---------:|---------:|---------:|----------:|---------:|--------:|----------:|
| ManyClasses | 1.612 ms | 0.0139 ms | 0.0130 ms | 142.5781 | 142.5781 | 142.5781 |   2.34 MB |

## AttributeDataModelGenerator

| Method   | Mean     | Error     | StdDev    | Gen0    | Gen1    | Allocated |
| -------- |---------:|----------:|----------:|--------:|--------:|----------:|
| RunAsync | 1.133 ms | 0.0182 ms | 0.0171 ms | 54.6875 | 11.7188 |   1.03 MB |

## EquatableArray

| Method                        | Count | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
| ------------------------------ |------ |------------:|----------:|----------:|------------:|------:|--------:|----------:|------------:|
| **EquatableArrayEquals**          | **10**    |   **4.1603 ns** | **0.1068 ns** | **0.1271 ns** |   **4.1864 ns** | **1.001** |    **0.04** |         **-** |          **NA** |
| EquatableArrayGetHashCode     | 10    |   0.1677 ns | 0.0091 ns | 0.0085 ns |   0.1688 ns | 0.040 |    0.00 |         - |          NA |
| ImmutableArrayReferenceEquals | 10    |   0.0032 ns | 0.0069 ns | 0.0061 ns |   0.0000 ns | 0.001 |    0.00 |         - |          NA |
| ImmutableArraySequenceEqual   | 10    |   3.7109 ns | 0.0374 ns | 0.0350 ns |   3.7080 ns | 0.893 |    0.03 |         - |          NA |
|                                |       |             |           |           |             |       |         |           |             |
| **EquatableArrayEquals**          | **100**   |  **29.9631 ns** | **0.2494 ns** | **0.2333 ns** |  **29.9944 ns** | **1.000** |    **0.01** |         **-** |          **NA** |
| EquatableArrayGetHashCode     | 100   |   0.1705 ns | 0.0086 ns | 0.0081 ns |   0.1685 ns | 0.006 |    0.00 |         - |          NA |
| ImmutableArrayReferenceEquals | 100   |   0.0029 ns | 0.0041 ns | 0.0038 ns |   0.0004 ns | 0.000 |    0.00 |         - |          NA |
| ImmutableArraySequenceEqual   | 100   |  29.3123 ns | 0.2250 ns | 0.2105 ns |  29.3094 ns | 0.978 |    0.01 |         - |          NA |
|                                |       |             |           |           |             |       |         |           |             |
| **EquatableArrayEquals**          | **1000**  | **189.2828 ns** | **1.3004 ns** | **1.2164 ns** | **189.2343 ns** | **1.000** |    **0.01** |         **-** |          **NA** |
| EquatableArrayGetHashCode     | 1000  |   0.1855 ns | 0.0109 ns | 0.0102 ns |   0.1817 ns | 0.001 |    0.00 |         - |          NA |
| ImmutableArrayReferenceEquals | 1000  |   0.0275 ns | 0.0178 ns | 0.0167 ns |   0.0251 ns | 0.000 |    0.00 |         - |          NA |
| ImmutableArraySequenceEqual   | 1000  | 191.8212 ns | 2.4799 ns | 2.3197 ns | 191.5713 ns | 1.013 |    0.01 |         - |          NA |

## ForAttributeTransform

| Method                    | Mean        | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
| -------------------------- |------------:|----------:|----------:|------:|-------:|----------:|------------:|
| GetDeclaredSymbolFromNode | 115.5924 ns | 0.8672 ns | 0.7688 ns | 1.000 | 0.0017 |      32 B |        1.00 |
| PreResolvedTargetSymbol   |   0.5658 ns | 0.0182 ns | 0.0170 ns | 0.005 |      - |         - |        0.00 |

## SourceGeneratorTestRunner

| Method   | ClassCount | CompileToAssembly | Mean          | Error       | StdDev      | Gen0    | Gen1    | Allocated  |
| -------- |----------- |------------------ |--------------:|------------:|------------:|--------:|--------:|-----------:|
| **RunAsync** | **1**          | **False**             |      **5.725 μs** |   **0.0552 μs** |   **0.0489 μs** |  **0.8545** |  **0.2136** |   **15.79 KB** |
| **RunAsync** | **1**          | **True**              |  **7,078.346 μs** | **133.0041 μs** | **124.4121 μs** | **23.4375** |  **7.8125** |  **458.51 KB** |
| **RunAsync** | **10**         | **False**             |      **7.579 μs** |   **0.0873 μs** |   **0.0729 μs** |  **1.0147** |  **0.2518** |   **18.73 KB** |
| **RunAsync** | **10**         | **True**              |  **7,746.954 μs** | **150.9553 μs** | **239.4311 μs** | **23.4375** |  **7.8125** |  **555.61 KB** |
| **RunAsync** | **100**        | **False**             |     **25.144 μs** |   **0.5020 μs** |   **0.5580 μs** |  **2.7161** |  **0.4272** |   **50.19 KB** |
| **RunAsync** | **100**        | **True**              | **10,294.152 μs** | **203.6992 μs** | **382.5964 μs** | **78.1250** | **15.6250** | **1552.67 KB** |

## TypeIdentity

| Method                | Mean     | Error    | StdDev   | Allocated |
| ---------------------- |---------:|---------:|---------:|----------:|
| Int32Identity         | 15.19 ns | 0.207 ns | 0.193 ns |         - |
| StringIdentity        | 14.85 ns | 0.266 ns | 0.236 ns |         - |
| ListOfStringIdentity  | 16.72 ns | 0.348 ns | 0.386 ns |         - |
| DictionaryIdentity    | 16.61 ns | 0.289 ns | 0.271 ns |         - |
| NestedGenericIdentity | 16.43 ns | 0.323 ns | 0.303 ns |         - |

## TypeLibraryGenerator

| Method   | SpecCount | Mean     | Error     | StdDev    | Gen0     | Gen1    | Allocated |
| -------- |---------- |---------:|----------:|----------:|---------:|--------:|----------:|
| **RunAsync** | **1**         | **1.958 ms** | **0.0297 ms** | **0.0278 ms** |  **62.5000** | **11.7188** |   **1.16 MB** |
| **RunAsync** | **5**         | **2.555 ms** | **0.0368 ms** | **0.0344 ms** |  **97.6563** | **23.4375** |   **1.81 MB** |
| **RunAsync** | **20**        | **4.913 ms** | **0.0965 ms** | **0.1414 ms** | **234.3750** | **39.0625** |   **4.27 MB** |

## Regenerating the results

Run the benchmarks project and copy the generated reports into the tables above:

```bash
dotnet run -c Release --project src/src/SourceGeneratorFramework.Benchmarks --framework net10.0
```

Filter to a single benchmark with `--filter "*Name*"`. The Markdown reports are written to
`BenchmarkDotNet.Artifacts/results/`.