using System;
using CDF;

public class CdfReader : IDisposable {
	public long Id;
	public int LastCommandStatus;         // Returned status code.

	private string _csharpCdfDir;

	public static CdfReader OpenFile(string path) {
		var reader = new CdfReader();
		reader.Open(path);
		return reader;
	}
	
	private void Open(string path) {
		LastCommandStatus = CDFAPIs.CDFopenCDF(path, out var locId);
		Id = locId;
	}

	public static string GetLibVersion() {
		CDFAPIs.CDFgetLibraryVersion(out var version, out var release, out var increment, out var subIncrement);
		return $"CDF Library Version: {version}.{release}.{increment}.{subIncrement}";
	}

	public string GetFileVersion() {
		LastCommandStatus = CDFAPIs.CDFgetVersion(Id, out var version, out var release, out var increment);
		return $"CDf file version: {version}.{release}.{increment}";
	}

	public void Dispose() {
		LastCommandStatus = CDFAPIs.CDFcloseCDF(Id);
		Id = 0L;
	}
}
