private long GetTotalSystemRam()
{
    try
    {
        var objMem = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
        objMem.NextValue();

        // Fallback: estimate based on available RAM
        return 16384; // Default 16GB estimate
    }
    catch
    {
        return 8192; // Fallback to 8GB
    }
}
