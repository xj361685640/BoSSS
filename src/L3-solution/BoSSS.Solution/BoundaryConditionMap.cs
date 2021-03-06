﻿/* =======================================================================
Copyright 2017 Technische Universitaet Darmstadt, Fachgebiet fuer Stroemungsdynamik (chair of fluid dynamics)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using BoSSS.Foundation.Grid;
using BoSSS.Foundation.Grid.Classic;
using BoSSS.Solution.Control;
using ilPSP.Tracing;
using MPI.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BoSSS.Solution.Utils {
    
    /// <summary>
    /// mapping from edge tags
    /// (<see cref="Foundation.Grid.GridData.EdgeData.EdgeTags"/>) to boundary
    /// condition types (defined in the enumeration
    /// <typeparamref name="BCType"/>) and function;
    /// </summary>
    /// <remarks>
    /// In BoSSS, different regions of the computational boundary (e.g. inlet,
    /// wall, moving wall,...) are identified by numbers (edge tags) which are
    /// assigned to the grid edges on the boundary. These numbers are small, in
    /// the region of 1 (including) to
    /// <see cref="BoSSS.Foundation.Grid.GridCommons.FIRST_PERIODIC_BC_TAG"/>;<br/>
    /// This edge-tag -- numbers can be used as <em>indices</em> into the 
    /// <list type="bullet">
    /// <item><see cref="EdgeTag2Type"/></item>
    /// <item><see cref="bndFunction"/></item>
    /// </list>
    /// -- arrays.
    /// </remarks>
    /// <typeparam name="BCType">
    /// Must be an enum, which defines the boundary condition types (e.g. Wall,
    /// Dirichlet, Neumann, ...) that are supported by the application; 
    /// </typeparam>
    public class BoundaryCondMap<BCType> where BCType : struct {

        /// <summary>
        /// keys: the names of the members of <typeparamref name="BCType"/>,
        /// i.e. all enum entries as strings;<br/>
        /// values: the corresponding <typeparamref name="BCType"/>-values;
        /// </summary>
        protected SortedDictionary<string, BCType> BoundaryCondTypes = new SortedDictionary<string, BCType>(new StringIgnoreCaseComp());

        /// <summary>
        /// comparer for <see cref="BoundaryCondTypes"/>
        /// </summary>
        class StringIgnoreCaseComp : IComparer<string> {

            #region IComparer<string> Members

            public int Compare(string x, string y) {
                return x.ToLowerInvariant().CompareTo(y.ToLowerInvariant());
            }

            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        class StringIgnoreCaseEq : IEqualityComparer<string> {

            #region IEqualityComparer<string> Members

            public bool Equals(string x, string y) {
                return x.Equals(y, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode(string obj) {
                return obj.ToLower().GetHashCode();
            }

            #endregion
        }


        /// <summary>
        /// scalar functions that encode boundary values;
        /// </summary>
        /// <remarks>
        /// Keys: User-defined names that were provided with the constructor
        /// (e.g. 'VelocityX', 'Pressure',...) <br/>
        /// Values: a function, to evaluate the BC values
        /// Array index: edge tag;
        /// </remarks>
        public Dictionary<string, Func<double[], double, double>[]> bndFunction = new Dictionary<string, Func<double[], double, double>[]>();

        GridCommons m_grid;
        GridData m_grdDat;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="grdDat"></param>
        /// <param name="_bndy"></param>
        /// <param name="bndFunctionNames">
        /// all names of boundary functions that should be parsed for;
        /// </param>
        public BoundaryCondMap(GridData grdDat, IDictionary<string, AppControl.BoundaryValueCollection> _bndy, params string[] bndFunctionNames) {

            var bndy = new Dictionary<string, AppControl.BoundaryValueCollection>(_bndy, new StringIgnoreCaseEq());

            m_grdDat = grdDat;
            m_grid = grdDat.Grid;
            var grid = m_grid;

            // verify Boundary Condition Type Enum
            // ====================================
            {

                Type tT = typeof(BCType);
                if (!tT.IsEnum)
                    throw new ApplicationException("generic type argument must be an enumeration.");

                var consts = tT.GetMembers();

                foreach (var c in consts) {

                    try {
                        BCType val;
                        val = (BCType)Enum.Parse(tT, c.Name); // if we pass this line, we've found a constant of the enum

                        BoundaryCondTypes.Add(c.Name, val);
                    } catch (Exception) {
                        // some other member, e.g. Equals(),...
                    }
                }

            }

            // verify boundary function names
            // ==============================

            foreach (string s in bndFunctionNames) {
                Func<double[], double, double>[] vals = new Func<double[], double, double>[GridCommons.FIRST_PERIODIC_BC_TAG];
                //ArrayTools.Set(vals, Zero);
                for (int ii = 0; ii < vals.Length; ii++)
                    vals[ii] = Zero;

                bndFunction.Add(s, vals);

            }


            // verification: test if EdgeTagsNames and EdgeTags in control file and in grid match
            // ==================================================================================

            //int D = grid.SpatialDimension;
            {
                List<string> Problems = new List<string>();


                // 1st: look if all EdgeTagsNames in the Grid have a corresponding entry in the control file
                foreach (byte EdgeTag in grid.EdgeTagNames.Keys) {  // loop over all edge tag names in grid ...
                    string EdgeTagName = grid.EdgeTagNames[EdgeTag];
                    if (EdgeTagName.Length <= 0)
                        Problems.Add("Found an empty EdgeTagName in the grid. (EdgeTag = " + EdgeTag + ").");

                    if (EdgeTag == 0) {
                        // must not be in the control file

                        foreach (string edgetagname in bndy.Keys)
                            if (edgetagname.Equals(EdgeTagName, StringComparison.InvariantCultureIgnoreCase))
                                Problems.Add("Illegal specification for boundary condition on internal edge: (EdgeTagName = '" + EdgeTagName + "', Edgetag = " + EdgeTag + ".");

                    } else if (EdgeTag >= GridCommons.FIRST_PERIODIC_BC_TAG) {
                        // must not be in the control file

                        foreach (string edgetagname in bndy.Keys)
                            if (edgetagname.Equals(EdgeTagName, StringComparison.InvariantCultureIgnoreCase))
                                Problems.Add("Illegal specification for boundary condition on periodic edge: (EdgeTagName = '" + EdgeTagName + "', Edgetag = " + EdgeTag + ".");

                    } else {
                        // must be contained in the control file

                        int found = 0;
                        foreach (string edgetagname in bndy.Keys)
                            if (edgetagname.Equals(EdgeTagName, StringComparison.InvariantCultureIgnoreCase))
                                found++;

                        if (found > 1)
                            Problems.Add("Boundary condition in control file is specified more than once (" + found + " times) (EdgeTagName = '" + EdgeTagName + "', Edgetag = " + EdgeTag + ".");

                        if (found == 0)
                            Problems.Add("EdgeTagName = '" + EdgeTagName + "', Edgetag = " + EdgeTag + " is specified in the grid, but not in control file.");
                    }
                }

                // 2nd: look if all boundary conditions in the control file can be found in the grid.
                foreach (string edgetagname in bndy.Keys) { // loop over all boundary conditions in control file
                    if (edgetagname == null || edgetagname.Length <= 0)
                        Problems.Add("found some boundary condition with zero-length name");

                    if (!grid.EdgeTagNames.Values.Contains(edgetagname, new StringIgnoreCaseEq())) {
                        Problems.Add("some boundary condition for '" + edgetagname + "' is specified in control file, but there is no corresponding EdgeTagName in the grid.");
                    }
                }



                // 3rd: test if all boundary conditions can be interpreted
                foreach (var bc in bndy) {
                    string edgetagname = bc.Key;
                    string _EdgeTagName = edgetagname.ToLowerInvariant();
                    string ttt = bc.Value.type;

                    int interptret = 0;
                    foreach (string bctype in this.BoundaryCondTypes.Keys) {
                        string _bctype = bctype.ToLowerInvariant();

                        if (ttt != null && ttt.Equals(bctype, StringComparison.InvariantCultureIgnoreCase)) {
                            interptret++;
                            continue;
                        }

                        if (ttt == null && _EdgeTagName.Contains(_bctype)) {
                            interptret++;
                            continue;
                        }
                    }

                    if (interptret <= 0)
                        Problems.Add("unable to interpret boundary condition for name '" + edgetagname + "': no fitting boundary condition type found;");
                    if (interptret > 1)
                        Problems.Add("unable to interpret boundary condition for name '" + edgetagname + "': not unique; ");

                }

                // throw an exception, if there are any problems
                if (Problems.Count > 0) {
                    StringWriter stw = new StringWriter();

                    stw.WriteLine("Found " + Problems.Count + " problem(s) with boundary condition specification.");
                    foreach (string s in Problems)
                        stw.WriteLine(s);

                    stw.Write("(EdgeTagNames in the grid are: ");
                    int cnt = 0;
                    foreach (string s in grid.EdgeTagNames.Values) {
                        cnt++;
                        stw.Write("'" + s + "'");
                        if (cnt < grid.EdgeTagNames.Count)
                            stw.Write(", ");
                    }
                    stw.Write(")");


                    throw new ApplicationException(stw.ToString());
                }

            }

            // test edges 
            // ==========

            // loop over all edges ...
            int E = grdDat.Edges.Count;
            for (int e = 0; e < E; e++) {
                //foreach (byte EdgeTag in grdDat.EdgeTags) {
                byte EdgeTag = grdDat.Edges.EdgeTags[e];

                if (EdgeTag == 0) {
                    if (grdDat.Edges.CellIndices[e, 1] < 0)
                        // unspec. EdgeTag on boundary -> error
                        throw new ApplicationException("Error in Grid: found at least one edge without EdgeTag;");
                    else
                        // inner edge: nothing to do;
                        continue;
                }

                if (EdgeTag >= GridCommons.FIRST_PERIODIC_BC_TAG)
                    continue; // do nothing for periodic edges

            }


            // build the EdgeTag -> BoundaryCondType -- Mapping
            // ================================================

            foreach (byte EdgeTag in grid.EdgeTagNames.Keys) {


                string EdgeTagName = grid.EdgeTagNames[EdgeTag];
                if (EdgeTag == 0 || EdgeTag >= GridCommons.FIRST_PERIODIC_BC_TAG)
                    continue;

                AppControl.BoundaryValueCollection bc = bndy[EdgeTagName];


                {
                    // these problems should have been detected prevoiusly...

                    if (bc == null)
                        throw new ApplicationException("unable to find definition for boundary in control file; " +
                            "From Gird: EdegTag = " + EdgeTag + ", EdgeTagName = " + EdgeTagName + "; ");
                    if (EdgeTagName == null || EdgeTagName.Length <= 0)
                        throw new ApplicationException("EdgeTagName is not defined: must be defined either in grid or within control-file; EdgeTag = " + EdgeTag);
                }

                // set boundary condition
                // ----------------------
                {
                    string _EdgeTagName = EdgeTagName.ToLowerInvariant();
                    string type = bc.type;
                    if (type == null)
                        type = "";
                    if (type.Length > 0)
                        // specification of type overrules the name
                        _EdgeTagName = "";

                    bool found = false;
                    foreach (string bctype in this.BoundaryCondTypes.Keys) {
                        if (type.Equals(bctype, StringComparison.InvariantCultureIgnoreCase) || _EdgeTagName.Contains(bctype.ToLowerInvariant())) {
                            EdgeTag2Type[EdgeTag] = this.BoundaryCondTypes[bctype];
                            found = true;
                            break;
                        }
                    }

                    if (!found) {
                        throw new ApplicationException("unable to determine boundary condition type:" +
                            " EdgeTagName: \"" + EdgeTagName + "\": " +
                            "EdgeTagName or type must contain either with \"Wall\", \"Velocity_inlet\", or \"Pressure_Outlet\", or \"Outflow\" (case insensitive) to be identified by this version of the Navier-Stokes solver.");
                    }


                    foreach (var kv in bc.Evaluators) {
                        string DesiredFieldName = kv.Key;
                        Func<double[], double, double> f_bnd = kv.Value;

                        if (!bndFunction.Keys.Contains(DesiredFieldName)) {
                            //string allNmn = "";
                            //foreach (var ss in bndFunction.Keys)
                            //    allNmn += ss + " ";
                            //throw new ApplicationException("boundary function '" + val.DesiredFieldName + "' is not supported by the application (must be one of: " + allNmn+");");

                            // there may be some other BoundaryConditionMap which uses this name

                        } else {
                            bndFunction[DesiredFieldName][EdgeTag] = f_bnd;
                        }
                    }

                }
            }

            // define which boundary condition types are used in the actual grid !!!
            // =====================================================================

            foreach (BCType bct in this.BoundaryCondTypes.Values) {


                int cnt = 0;

                // loop over all edges ...
                E = grdDat.Edges.Count;
                for (int e = 0; e < E; e++) {
                    //foreach (byte EdgeTag in grdDat.EdgeTags) {
                    byte EdgeTag = grdDat.Edges.EdgeTags[e];

                    if (EdgeTag == 0)
                        continue;

                    if (EdgeTag >= GridCommons.FIRST_PERIODIC_BC_TAG)
                        continue; // do nothing for periodic edges

                    if (EdgeTag2Type[EdgeTag].Equals(bct))
                        cnt++;

                }

                int GlobCnt = 0;
                unsafe
                {
                    csMPI.Raw.Allreduce((IntPtr)(&cnt), (IntPtr)(&GlobCnt), 1,
                        csMPI.Raw._DATATYPE.INT, csMPI.Raw._OP.SUM, csMPI.Raw._COMM.WORLD);

                }

                BCTypeUseCount.Add(bct, GlobCnt);

            }
        }

        /// <summary>
        /// Keys: boundary condition type;<br/>
        /// Values: number of edges in the actual grid that make use of this boundary condition.
        /// </summary>
        public Dictionary<BCType, int> BCTypeUseCount = new Dictionary<BCType, int>();


        /// <summary>
        /// a function, which returns always 0.0;
        /// </summary>
        /// <param name="x"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private static double Zero(double[] x, double t) {
            return 0.0;
        }

        /// <summary>
        /// Map: EdgeTag (see
        /// <see cref="GridData.EdgeData.EdgeTags"/>) to boundary
        /// condition type
        /// </summary>
        public BCType[] EdgeTag2Type = new BCType[GridCommons.FIRST_PERIODIC_BC_TAG];

        /// <summary>
        /// true if the grid contains any periodic edge
        /// </summary>
        public bool ContainsPeriodicEdge {
            get {
                using (new FuncTrace()) {
                    ilPSP.MPICollectiveWatchDog.Watch(MPI.Wrappers.csMPI.Raw._COMM.WORLD);
                    int Cnt = 0, J = this.m_grdDat.Edges.Count;

                    for (int j = 0; j < J; j++) {
                        if (m_grdDat.Edges.EdgeTags[j] >= GridCommons.FIRST_PERIODIC_BC_TAG)
                            Cnt++;
                    }

                    unsafe
                    {
                        int CntTot = 0;
                        MPI.Wrappers.csMPI.Raw.Allreduce((IntPtr)(&Cnt), (IntPtr)(&CntTot), 1, MPI.Wrappers.csMPI.Raw._DATATYPE.INT, MPI.Wrappers.csMPI.Raw._OP.SUM, MPI.Wrappers.csMPI.Raw._COMM.WORLD);

                        return (CntTot > 0);
                    }
                }
            }

        }

    }

}
