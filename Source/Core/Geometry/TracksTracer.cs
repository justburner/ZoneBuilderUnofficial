using System;
using System.Collections.Generic;
using CodeImp.DoomBuilder.Map;
using System.Collections.ObjectModel;
using CodeImp.DoomBuilder.IO;

//JBR Tracks tracer
namespace CodeImp.DoomBuilder.Geometry
{
    public class TracksTracer
    {
        private List< List<Vector2D> > openpaths;
        private List< List<Vector2D> > closepaths;
        private bool isvalid;

        public List<List<Vector2D>> OpenPaths { get { return openpaths; } }
        public List<List<Vector2D>> ClosePaths { get { return closepaths; } }
        public bool IsValid { get { return isvalid; } }

        #region ==== Public methods ====

        /// <summary>
        /// Create tracks tracer class
        /// </summary>
        /// <param name="lines">Linedefs to trace</param>
        public TracksTracer(ICollection<Linedef> lines)
        {
            isvalid = true;

            // Get all vertices connected to the linedefs
            List<Vertex> allvertices = new List<Vertex>();
            foreach (Linedef ld in lines)
            {
                if (!allvertices.Contains(ld.Start)) allvertices.Add(ld.Start);
                if (!allvertices.Contains(ld.End)) allvertices.Add(ld.End);
            }

            // Create a list of valid vertices
            List<Vertex> vertices = new List<Vertex>();
            foreach (Vertex v in allvertices)
            {
                List<Linedef> list = GetSelectedLinedefFromVertex(lines, v);
                if (list.Count == 1 || list.Count == 2) vertices.Add(v);
            }

            // Scan for open paths
            openpaths = new List< List<Vector2D> >();
            List<Vector2D> processed = new List<Vector2D>(); // for holding processed locations
            foreach (Vertex v in vertices)
            {
                if (!processed.Contains(v.Position))
                {
                    List<Linedef> list1 = GetSelectedLinedefFromVertex(lines, v);
                    if (list1.Count == 1)
                    {
                        List<Vector2D> path = new List<Vector2D>();
                        processed.Add(v.Position);
                        path.Add(v.Position);
                        Vertex v2 = (list1[0].Start == v) ? list1[0].End : list1[0].Start;
                        bool reverse = (list1[0].Start == v);
                        do
                        {
                            processed.Add(v2.Position);
                            path.Add(v2.Position);
                            List<Linedef> list2 = GetSelectedLinedefFromVertex(lines, v2);
                            if (list2.Count == 1)
                            {
                                // Linedef continue on the opposide side
                                v2 = (list2[0].Start == v2) ? list2[0].End : list2[0].Start;
                            }
                            else if (list2.Count == 2)
                            {
                                // Linedef continue in one of the opposide sides
                                Vertex v3 = (list2[0].Start == v2) ? list2[0].End : list2[0].Start;
                                if (processed.Contains(v3.Position))
                                {
                                    v2 = (list2[1].Start == v2) ? list2[1].End : list2[1].Start;
                                }
                                else
                                {
                                    v2 = v3;
                                }
                            }
                            else
                            {
                                // Collided with a 3+ way vertex, no good...
                                isvalid = false;
                                break;
                            }
                        } while (!processed.Contains(v2.Position));
                        if (reverse) path.Reverse();
                        openpaths.Add(path);
                    }
                }
            }

            // Scan for close paths
            closepaths = new List<List<Vector2D>>();
            foreach (Vertex v in vertices)
            {
                if (!processed.Contains(v.Position))
                {
                    List<Linedef> list1 = GetSelectedLinedefFromVertex(lines, v);
                    List<Vector2D> path = new List<Vector2D>();
                    processed.Add(v.Position);
                    path.Add(v.Position);
                    Vertex v2 = (list1[0].Start == v) ? list1[0].End : list1[0].Start;
                    do
                    {
                        processed.Add(v2.Position);
                        path.Add(v2.Position);
                        List<Linedef> list2 = GetSelectedLinedefFromVertex(lines, v2);
                        if (list2.Count == 2)
                        {
                            // Linedef continue in one of the opposide sides
                            Vertex v3 = (list2[0].Start == v2) ? list2[0].End : list2[0].Start;
                            if (processed.Contains(v3.Position))
                            {
                                v2 = (list2[1].Start == v2) ? list2[1].End : list2[1].Start;
                            }
                            else
                            {
                                v2 = v3;
                            }
                        }
                        else
                        {
                            // Collided with a 3+ way vertex, no good...
                            isvalid = false;
                            break;
                        }
                    } while (!processed.Contains(v2.Position));
                    path.Add(path[0]); // Close it by looping back to 0
                    if (!InsideCheckFlip(path)) path.Reverse(); // Reverse if parallel was created outside
                    closepaths.Add(path);
                }
            }
        }

        /// <summary>
        /// Parallelize open-path, used to create roads
        /// </summary>
        /// <param name="path">Path in OpenPaths</param>
        /// <param name="distance">Distance between parallels / Width of the track</param>
        /// <param name="flip">Flip to the other side?</param>
        /// <returns>Parallel path</returns>
        public static List<Vector2D> ParallelizeOpenPath(List<Vector2D> path, float distance, bool flip)
        {
            List<Vector2D> parallel = new List<Vector2D>();
            if (path.Count < 2) return parallel;
            parallel.Add(GetPerpendicular3(path[0], path[0], path[1], distance, flip));
            int i = 2;
            for (; i < path.Count; i++)
            {
                parallel.Add(GetPerpendicular3(path[i - 2], path[i - 1], path[i], distance, flip));
            }
            parallel.Add(GetPerpendicular3(path[i - 2], path[i - 1], path[i - 1], distance, flip));
            return parallel;
        }

        /// <summary>
        /// Parallelize close-path, used to create roads
        /// </summary>
        /// <param name="path">Path in ClosePaths</param>
        /// <param name="distance">Distance between parallels / Width of the track</param>
        /// <param name="flip">Flip to the other side?</param>
        /// <returns>Parallel path</returns>
        public static List<Vector2D> ParallelizeClosePath(List<Vector2D> path, float distance, bool flip)
        {
            List<Vector2D> parallel = new List<Vector2D>();
            if (path.Count < 2) return parallel;
            int i = path.Count;
            parallel.Add(GetPerpendicular3(path[i - 2], path[0], path[1], distance, flip));
            for (i = 2; i < path.Count; i++)
            {
                parallel.Add(GetPerpendicular3(path[i - 2], path[i - 1], path[i], distance, flip));
            }
            parallel.Add(GetPerpendicular3(path[i - 2], path[i - 1], path[1], distance, flip));
            return parallel;
        }

        /// <summary>
        /// Add path into DrawnVertex list
        /// </summary>
        /// <param name="dvl">DrawnVertex list</param>
        /// <param name="path">Path to add</param>
        /// <param name="stitch">Stitch?</param>
        /// <param name="stitchline">Stitchline?</param>
        /// <returns>Number of elements added</returns>
        public static int AddPathToDrawnVertex(List<DrawnVertex> dvl, List<Vector2D> path, bool stitch, bool stitchline)
        {
            int elements = 0;
            DrawnVertex newpoint = new DrawnVertex(); // struct
            newpoint.stitch = stitch;
            newpoint.stitchline = stitchline;
            foreach (Vector2D pos in path)
            {
                if (pos.x < General.Map.Config.LeftBoundary || pos.x > General.Map.Config.RightBoundary ||
                    pos.y > General.Map.Config.TopBoundary || pos.y < General.Map.Config.BottomBoundary)
                    continue;

                newpoint.pos = pos;
                dvl.Add(newpoint);
                elements++;
            }
            return elements;
        }

        /// <summary>
        /// Add point into DrawnVertex list
        /// </summary>
        /// <param name="dvl">DrawnVertex list</param>
        /// <param name="pos">Point to add</param>
        /// <param name="stitch">Stitch?</param>
        /// <param name="stitchline">Stitchline?</param>
        /// <returns>Number of elements added, 1 if success</returns>
        public static int AddPointToDrawnVertex(List<DrawnVertex> dvl, Vector2D pos, bool stitch, bool stitchline)
        {
            if (pos.x < General.Map.Config.LeftBoundary || pos.x > General.Map.Config.RightBoundary ||
                pos.y > General.Map.Config.TopBoundary || pos.y < General.Map.Config.BottomBoundary)
                return 0;

            DrawnVertex newpoint = new DrawnVertex();
            newpoint.pos = pos;
            newpoint.stitch = stitch;
            newpoint.stitchline = stitchline;
            dvl.Add(newpoint);
            return 1;
        }

        #endregion

        #region ==== Private methods ====

        private bool InsideCheckFlip(List<Vector2D> track)
        {
            if (track.Count < 2) return false;

            // Calculate median point
            double x = 0, y = 0;
            foreach (Vector2D v in track)
            {
                x += v.x;
                y += v.y;
            }
            x /= track.Count;
            y /= track.Count;
            Vector2D median = new Vector2D((float)x, (float)y);

            // See which side is the first track line
            Line2D line = new Line2D(track[0], track[1]);
            return line.GetSideOfLine(median) >= 0;
        }

        /// <summary>
        /// Get selected linedefs from a vertex
        /// </summary>
        /// <param name="lines">Collection of lines</param>
        /// <param name="v">Vertex to get linedefs from</param>
        /// <returns>List of all selected linedefs</returns>
        private List<Linedef> GetSelectedLinedefFromVertex(ICollection<Linedef> lines, Vertex v)
        {
            List<Linedef> list = new List<Linedef>();
            foreach (Linedef ld in v.Linedefs)
            {
                if (lines.Contains(ld)) list.Add(ld);
            }
            return list;
        }

        /// <summary>
        /// Create line on the center vertex that is perpendicular to left and right vertices
        /// </summary>
        /// <param name="left">Left vertex</param>
        /// <param name="center">Center vertex</param>
        /// <param name="right">Right vertex</param>
        /// <param name="scale">Scale of the perpendicular line</param>
        /// <param name="backwards">Should perpendicular go backwards?</param>
        /// <returns>Position of the perpendicular</returns>
        private static Vector2D GetPerpendicular3(Vector2D left, Vector2D center, Vector2D right, float scale, bool backwards)
        {
            Vector2D deltapos = (right - center).GetNormal() - (left - center).GetNormal();
            if (backwards) deltapos = -deltapos;
            Vector2D normalunit = deltapos.GetPerpendicular().GetNormal();
            return center + normalunit * scale;
        }

        #endregion
    }
}
