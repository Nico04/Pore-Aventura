using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SolidBoundariesBuilder : Builder {
    public GameObject ObjectToPopulate;

    protected override async Task Build (CancellationToken cancellationToken) {
		var positions = await Task.Run(() => DataBase.GetSolidBoundaries(), cancellationToken).ConfigureAwait(true);

		foreach (Vector3 position in positions) {
	        Instantiate(ObjectToPopulate, position, Quaternion.identity, transform);
        }
	}
}
