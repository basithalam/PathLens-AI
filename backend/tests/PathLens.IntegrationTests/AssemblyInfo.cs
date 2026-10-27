using Xunit;

// Integration tests share local PostgreSQL databases and background job
// processors, so they must not run in parallel with each other.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
