using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SolidBoundariesBuilder : Builder {
    public GameObject ObjectToPopulate;

    public Vector3[] Positions;

    protected override async Task Build (CancellationToken cancellationToken) {
	    Positions = await Task.Run(() => DataBase.GetSolidBoundaries(), cancellationToken).ConfigureAwait(true);

		foreach (Vector3 position in Positions) {
	        Instantiate(ObjectToPopulate, position, Quaternion.identity, transform);
        }
	}
}
