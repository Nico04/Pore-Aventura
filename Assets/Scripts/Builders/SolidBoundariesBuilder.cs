using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SolidBoundariesBuilder : Builder {
	public GameObject ObjectToPopulate;

    public static Vector3[] Positions;
    protected override void Start() {
	    base.Start();

	    Positions = DataBase.SolidBoundaries;
	}

    protected override async Task Build (CancellationToken cancellationToken) {
		foreach (Vector3 position in Positions) {
	        Instantiate(ObjectToPopulate, position, Quaternion.identity, transform);
        }
	}

	//Check whether the point in inside a solid boundary 
	public static bool Contains(Vector3 point) {
		foreach (var center in Positions) {
			if (Vector3.Distance(point, center) < DataBase.SolidBoundaryRadius)
				return true;
		}

		return false;
	}
}
