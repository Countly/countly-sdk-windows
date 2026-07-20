// Init-based tests drive the shared static Countly singleton, so serialize the whole assembly.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
