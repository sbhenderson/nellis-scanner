using System;

namespace NellisScanner.Core.Models;

public enum NellisLocations
{
    LasVegas,
    Phoenix,
    Houston,
    Philadelphia,
    Denver,
    Dallas
}
public static class LocationCookie
{
    /// <summary>
    /// Fetch the url-decoded location cookie for a given Nellis location. These were manually found.
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string GetLocationCookie(NellisLocations location) => location switch
    {
        NellisLocations.LasVegas => "eyJzaG9wcGluZ0xvY2F0aW9uIjp7ImlkIjoxLCJuYW1lIjoiTGFzIFZlZ2FzLCBOViIsImxvY2F0aW9uUGhvdG8iOltdfX0=.XnEACH8r6fQr8vpoJUXKef+eCBg3byb8CF6UgSgO+3w",
        NellisLocations.Phoenix => "eyJzaG9wcGluZ0xvY2F0aW9uIjp7ImlkIjoyLCJuYW1lIjoiUGhvZW5peCwgQVoiLCJsb2NhdGlvblBob3RvIjpbXX19.jndmip+cG/iHXVo1hPJSilflQl2tNkln7JPND9ryMvo",
        NellisLocations.Houston => "eyJzaG9wcGluZ0xvY2F0aW9uIjp7ImlkIjo1LCJuYW1lIjoiSG91c3RvbiwgVFgiLCJsb2NhdGlvblBob3RvIjpbXX19.FrTnwjk0KEuJsmwDAvxyEI3s3Lcs63iP/gC9qKx4QRI",
        NellisLocations.Philadelphia => "eyJzaG9wcGluZ0xvY2F0aW9uIjp7ImlkIjo2LCJuYW1lIjoiUGhpbGFkZWxwaGlhLCBQQSIsImxvY2F0aW9uUGhvdG8iOltdfX0=.sb3FDRm/NqJCX3/3mymHLky/tUI4jlLfQGrc7O9OIOw",
        NellisLocations.Dallas => "eyJzaG9wcGluZ0xvY2F0aW9uIjp7ImlkIjo4LCJuYW1lIjoiRGFsbGFzLCBUWCIsImxvY2F0aW9uUGhvdG8iOltdfX0=.C9Xl7+efcVBDouv/WSRHiFNEl8soK4fQl0mkxK3YuBA",
        NellisLocations.Denver => "eyJzaG9wcGluZ0xvY2F0aW9uIjp7ImlkIjo3LCJuYW1lIjoiRGVudmVyLCBDTyIsImxvY2F0aW9uUGhvdG8iOltdfX0=.fh3TnhcLcJy0I5303uG4GfqrvJ6ynZVY2HDcAxBJt4o",
        _ => throw new ArgumentOutOfRangeException(nameof(location), location, null)
    };
}
