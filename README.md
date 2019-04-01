# Pore Aventura
<img src="/Assets/Images/logo.png" width="200"/>

## Description
Pore Aventura is a *3D velocity field explorer*, designed for academic research purposes. It aims at visualizing and investigating the kinematic properties of a steady 3D velocity field, which can contain solid stationary boundaries, like for instance in a porous media.

Among its features, Pore Aventura allows :
* 3D motion through the velocity field
* Local injection of passive advective tracers which flow following their streamline
* Upstream injection grid spawning advective tracers with a tunable grid resolution and spawn delay
* Streamlines visualization
* Roller-coaster mode, flowing the user following its local streamline (like a passive advective tracer)

Pore Aventura was initially developed to investigate dispersion and mixing in porous media, by providing a tool to explore the velocity intermittency, the streamlines tortuosity and the dispersion properties of an experimentally measured 3D velocity field obtained in a porous medium made out of a random stack of monodisperse rigid spherical beads.

## Media
Images

<a href="/Resources/Presentation/PA-illustr1.png">
	<img src="/Resources/Presentation/PA-illustr1.png" width="300" border="10"/>
</a>
<a href="/Resources/Presentation/PA-illustr2.png">
	<img src="/Resources/Presentation/PA-illustr2.png" width="300" border="10"/>
</a>

Gifs

![gif](/Resources/Presentation/PA-22.gif)
![gif](/Resources/Presentation/PA-3.gif)

![gif](/Resources/Presentation/PA-RollerCoaster.gif)
![gif](/Resources/Presentation/PA-Sheet.gif)

Video Teaser

<a href="http://www.youtube.com/watch?feature=player_embedded&v=VXmrcWyAC9Q" target="_blank">
	<img src="http://img.youtube.com/vi/VXmrcWyAC9Q/0.jpg" alt="Pore Aventura Teaser" width="240" border="10" />
</a>

Video Presentation

<a href="http://www.youtube.com/watch?feature=player_embedded&v=NkHmAcAheAQ" target="_blank">
	<img src="http://img.youtube.com/vi/NkHmAcAheAQ/0.jpg" alt="Pore Aventura Teaser" width="240" border="10" />
</a>

## Installation
1. Download the latest Pore Aventura release.
2. Extract downloaded archive.
3. Download the scientific data (data.h5) from the release page (or use a custom one).
4. Copy the the data.h5 file in the StreamingAssets folder of the extracted folder.
5. Enjoy !

## Creating a custom compatible Data.h5 data set
User can provide his own custom dataset by creating a hdf5 format file. The dataset should be named Data.h5, and contains 2 variables :
- SolidBoundaries => dimensions : N_s x 3, containing the position (x,y,z) of the centers of the N_s solid spheres forming the porous medium
- VelocityField => dimensions : M x N x P x 3, containing for each voxel (M,N,P) the 3 velocity components. The voxels contained in a solid sphere should have their 3 velocity components to 0.

### How to create a dataset using Matlab? 
It is very easy to export data in .h5 using the following line command :
```h5create('Data.h5', '/SolidBoundaries', size(SolidBoundaries), 'Datatype','single')
h5write('Data.h5', '/SolidBoundaries', SolidBoundaries)
h5create('Data.h5', '/VelocityField', size(VelocityField), 'Datatype','single')
h5write('Data.h5', '/VelocityField', VelocityField)
```

*Important notes :*
- The data.h5 file should then be added to the StreamingAssets folder
- In the previous example, datas are saved in single format
- It has been reported that Matlab reverse the variable dimensions when using h5write; therefore, the variables dimensions in Matlab Workspace should be reversed before saving in h5 format (i.e. SolidBoundaries should be 3 x N_s, and VelocityField should be 3 x P x N x M)

## Notes
Further releases should include the options for the N_s spheres to have different radii, a space_resolution parameter indicating the 3D velocity field spatial resolution, a Space_factor and a Time_factor parameters allowing to expand or compress the spatial and time dimension, and the option to take into account solid boundaries which are not resulting from a stack of spherical beads.