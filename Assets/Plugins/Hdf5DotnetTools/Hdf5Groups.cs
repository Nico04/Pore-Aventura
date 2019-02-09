using System;
using System.Collections.Generic;
using HDF.PInvoke;

namespace Hdf5DotNetTools {
	public static partial class Hdf5 {
		public static int CloseGroup(long groupId) {
			return H5G.close(groupId);
		}

		public static long CreateGroup(long groupId, string groupName) {
			long gid;
			if (GroupExists(groupId, groupName))
				gid = H5G.open(groupId, groupName);
			else
				gid = H5G.create(groupId, groupName);
			return gid;
		}

		/// <summary>
		/// creates a structure of groups at once
		/// </summary>
		/// <param name="groupId"></param>
		/// <param name="groupName"></param>
		/// <returns></returns>
		public static long CreateGroupRecursively(long groupId, string groupName) {
			IEnumerable<string> grps = groupName.Split('/');
			long gid = groupId;
			groupName = "";
			foreach (var name in grps) {
				groupName = string.Concat(groupName, "/", name);
				gid = CreateGroup(gid, groupName);
			}
			return gid;
		}

		public static bool GroupExists(long groupId, string groupName) {
			bool exists = false;
			try {
				H5G.info_t info = new H5G.info_t();
				var gid = H5G.get_info_by_name(groupId, groupName, ref info);
				exists = gid == 0;
			} catch (Exception) {
			}
			return exists;
		}

		public static ulong NumberOfAttributes(int groupId, string groupName) {
			H5O.info_t info = new H5O.info_t();
			var gid = H5O.get_info(groupId, ref info);
			return info.num_attrs;
		}

		public static H5O.info_t GroupInfo(long groupId) {
			H5O.info_t info = new H5O.info_t();
			var gid = H5O.get_info(groupId, ref info);
			return info;
		}
	}
}
