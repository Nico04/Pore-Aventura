using System;
using System.Linq;
using System.Runtime.InteropServices;
using HDF.PInvoke;

/// <summary>
/// Helper to read simple data sets of a HDF5 file
/// Use the Official HDF.PInvoke library
/// 
/// It's a small part of the Hdf5DotNetTools library : 
/// https://github.com/reyntjesr/Hdf5DotnetTools/tree/master/Hdf5DotNetTools
/// </summary>
public static class HdfReader {
	/// <summary>
	/// Opens a Hdf-5 file
	/// </summary>
	/// <param name="filename"></param>
	/// <param name="readOnly"></param>
	/// <returns></returns>
	public static long OpenFile(string filename, bool readOnly = false, bool overwrite = false) {
		uint access = (readOnly) ? H5F.ACC_RDONLY : H5F.ACC_RDWR;
		var fileId = H5F.open(filename, access);
		return fileId;
	}

	/// <summary>
	/// Reads an n-dimensional dataSet.
	/// </summary>
	/// <typeparam name="T">Generic parameter strings or primitive type</typeparam>
	/// <param name="groupId">id of the group. Can also be a file Id</param>
	/// <param name="name">name of the dataSet</param>
	/// <returns>The n-dimensional dataSet</returns>
	public static Array ReadDataSetToArray<T>(long groupId, string name) {
		var dataType = GetDataType(typeof(T));

		var dataSetId = H5D.open(groupId, name);
		var spaceId = H5D.get_space(dataSetId);
		int rank = H5S.get_simple_extent_ndims(spaceId);
		long count = H5S.get_simple_extent_npoints(spaceId);
		Array dset;
		Type type = typeof(T);
		if (rank >= 0 && count >= 0) {
			int rankChunk;
			ulong[] maxDims = new ulong[rank];
			ulong[] dims = new ulong[rank];
			ulong[] chunkDims = new ulong[rank];
			long memId = H5S.get_simple_extent_dims(spaceId, dims, maxDims);
			long[] lengths = dims.Select(Convert.ToInt64).ToArray();
			dset = Array.CreateInstance(type, lengths);
			var typeId = H5D.get_type(dataSetId);
			var mem_type = H5T.copy(dataType);
			if (dataType == H5T.C_S1)
				H5T.set_size(dataType, new IntPtr(2));

			var propId = H5D.get_create_plist(dataSetId);

			if (H5D.layout_t.CHUNKED == H5P.get_layout(propId))
				rankChunk = H5P.get_chunk(propId, rank, chunkDims);

			memId = H5S.create_simple(rank, dims, maxDims);
			GCHandle hnd = GCHandle.Alloc(dset, GCHandleType.Pinned);
			H5D.read(dataSetId, dataType, memId, spaceId,
				H5P.DEFAULT, hnd.AddrOfPinnedObject());
			hnd.Free();
		} else
			dset = Array.CreateInstance(type, new long[1] { 0 });
		H5D.close(dataSetId);
		H5S.close(spaceId);
		return dset;

	}

	internal static long GetDataType(System.Type type) {
		//var typeName = type.Name;
		long dataType;

		var typeCode = Type.GetTypeCode(type);
		switch (typeCode) {
			case TypeCode.Byte:
				dataType = H5T.NATIVE_INT8;
				break;
			case TypeCode.SByte:
				dataType = H5T.NATIVE_UINT8;
				break;
			case TypeCode.Int16:
				dataType = H5T.NATIVE_INT16;
				break;
			case TypeCode.Int32:
				dataType = H5T.NATIVE_INT32;
				break;
			case TypeCode.Int64:
				dataType = H5T.NATIVE_INT64;
				break;
			case TypeCode.UInt16:
				dataType = H5T.NATIVE_UINT16;
				break;
			case TypeCode.UInt32:
				dataType = H5T.NATIVE_UINT32;
				break;
			case TypeCode.UInt64:
				dataType = H5T.NATIVE_UINT64;
				break;
			case TypeCode.Single:
				dataType = H5T.NATIVE_FLOAT;
				break;
			case TypeCode.Double:
				dataType = H5T.NATIVE_DOUBLE;
				break;
			case TypeCode.Char:
				dataType = H5T.C_S1;
				break;
			case TypeCode.String:
				dataType = H5T.C_S1;
				break;
			default:
				throw new Exception($"Data Type {type} not supported");
		}
		return dataType;
	}
}
