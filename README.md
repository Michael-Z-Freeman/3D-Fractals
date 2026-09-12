# 3D Fractals

This Unity project contains a performance-bounded, ray-marched Menger sponge. The apparent tunnels and repeated cavities are calculated per pixel on the GPU. They are not represented by a detailed mesh, a hierarchy of generated cubes, colliders, or triangle geometry.

![Ray-marched Menger sponge](Assets/Screenshots/FractalLab_Raymarched.png)

## Open the example

Open **Assets/Scenes/FractalLab.unity** and select **UberMenger_Raymarched** in the Hierarchy.

The selected object is an ordinary Unity cube with a Mesh Filter and Mesh Renderer. The cube is only a bounded carrier volume. Its renderer uses **Assets/Fractals/MengerRaymarch.mat**, which selects the custom shader **Assets/Fractals/Shaders/BoundedMengerRaymarch.shader**.

## The Menger sponge mathematics

The Menger sponge begins as a cube. At every iteration, divide every retained cube into a 3 x 3 x 3 grid and remove the central cube plus the six face-centre cubes. Each retained cube therefore creates 20 smaller retained cubes.

If S_0 is the initial cube and D = {-1, 0, 1}^3, the recursive construction can be written as:

$$
S_{n+1} = \bigcup_{(i,j,k) \in D,\; N_0(i,j,k) \le 1}
\left( \frac{S_n + (i,j,k)}{3} \right)
$$

Here, N_0(i, j, k) is the number of coordinates equal to zero. The condition N_0(i, j, k) <= 1 retains the 20 cells with at most one centred coordinate and removes the seven cells having two or three centred coordinates. After n construction iterations, the idealized form contains 20^n retained subcubes. Its fractal dimension is:

$$
D = \frac{\log 20}{\log 3} \approx 2.7268
$$

This is a recursive geometric fractal. It is not the Mandelbrot recurrence z_(n+1) = z_n^2 + c: there is no evolving complex-number orbit whose output is fed into the next iteration. Instead, each scale repeats the cube-and-cross removal rule.

## How the shader renders it

The shader evaluates a signed-distance approximation rather than generating cube meshes. It starts with the signed distance to the bounding cube:

$$
d_{box}(p,b) = \lVert \max(\lvert p \rvert-b,0) \rVert + \min(\max(q_x,\max(q_y,q_z)),0), \quad q=\lvert p \rvert-b
$$

At each Menger scale, it computes three thin, perpendicular box distances for the local cell. Their union is the cross-shaped hole. Boolean subtraction of that hole from the retained solid is expressed with signed distances as:

$$
d_{new}(p) = \max\left(d_{old}(p),-d_{cross}(p)\right)
$$

For every covered pixel, the shader then:

1. Transforms the camera ray into the cube's local space.
2. Intersects it with the proxy cube, producing a strict entry and exit interval.
3. Repeatedly evaluates the Menger signed-distance function and advances by that distance.
4. Stops on a surface hit, when the ray leaves the cube, or when the configured step limit is reached.
5. Estimates a normal from nearby distance samples and applies simple directional lighting and edge glow.

The main ray loop has a compile-time ceiling of 128 steps. The material default is deliberately lower: 64 primary steps and 3 Menger iterations. There are no secondary ray-marched shadow or ambient-occlusion passes in this first version.

## Files to inspect

| File | Role |
| --- | --- |
| [Assets/Scenes/FractalLab.unity](Assets/Scenes/FractalLab.unity) | Example scene containing the single cube proxy object. |
| [Assets/Fractals/MengerRaymarch.mat](Assets/Fractals/MengerRaymarch.mat) | Editable Unity material: iterations, max ray steps, epsilon, step scale, colours, and light direction. |
| [Assets/Fractals/Shaders/BoundedMengerRaymarch.shader](Assets/Fractals/Shaders/BoundedMengerRaymarch.shader) | HLSL implementation of the bounding-box intersection, Menger distance function, ray loop, and normal estimation. |
| [Assets/Screenshots/FractalLab_Raymarched.png](Assets/Screenshots/FractalLab_Raymarched.png) | Captured result shown above. |

## Performance controls

The most important quality/performance controls on MengerRaymarch are:

- **Fractal Iterations**: more repeated geometric detail; increases the cost of every distance evaluation.
- **Maximum Ray Steps**: maximum primary ray-march iterations per shaded pixel.
- **Surface Epsilon**: hit tolerance; larger values are faster/stabler but soften fine detail.
- **Step Scale**: fraction of the signed-distance step to use; lower values are safer but require more iterations.
- **Maximum Local Distance**: additional hard bound on travel inside the proxy volume.

Keep the proxy volume tight around the desired effect. The shader only runs on pixels rasterized by that cube, and the box intersection prevents it from marching through empty scene space.
