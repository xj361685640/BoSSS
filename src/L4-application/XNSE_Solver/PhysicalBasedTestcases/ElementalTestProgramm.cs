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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using MPI.Wrappers;
using BoSSS.Solution;
using BoSSS.Solution.XdgTimestepping;


namespace BoSSS.Application.XNSE_Solver.Tests {

    [TestFixture]
    static class ElementalTestProgramm {

        [TestFixtureSetUp]
        public static void Init() {
            bool dummy;
            ilPSP.Environment.Bootstrap(
                new string[0],
                BoSSS.Solution.Application.GetBoSSSInstallDir(),
                out dummy);
        }

        [TestFixtureTearDown]
        public static void Cleanup() {

        }


        // ==========================
        // Check boundary conditions
        // ==========================



        // ========================
        // Check level set movement
        // ========================

        //[Test]
        //public static void LineMovementTest(
        //    [Values(LevelSetEvolution.FastMarching, LevelSetEvolution.ExtensionVelocity)]  LevelSetEvolution lsEvo,
        //    [Values(LevelSetHandling.LieSplitting, LevelSetHandling.Coupled_Once, LevelSetHandling.Coupled_Iterative)] LevelSetHandling lsHandl,
        //    [Values(XNSE_Control.TimesteppingScheme.ImplicitEuler, XNSE_Control.TimesteppingScheme.CrankNicolson, XNSE_Control.TimesteppingScheme.BDF2)] XNSE_Control.TimesteppingScheme tsScheme) {

        //    var C = PhysicalBasedTestcases.ChannelFlow.CF_LevelSetMovementTest(1, lsEvo, lsHandl, tsScheme);

        //}

        //[Test]
        //public static void CircleMovementTest(
        //    [Values(LevelSetEvolution.FastMarching, LevelSetEvolution.ExtensionVelocity)]  LevelSetEvolution lsEvo,
        //    [Values(LevelSetHandling.LieSplitting, LevelSetHandling.Coupled_Once, LevelSetHandling.Coupled_Iterative)] LevelSetHandling lsHandl,
        //    [Values(XNSE_Control.TimesteppingScheme.ImplicitEuler, XNSE_Control.TimesteppingScheme.CrankNicolson, XNSE_Control.TimesteppingScheme.BDF2)] XNSE_Control.TimesteppingScheme tsScheme) {

        //    var C = PhysicalBasedTestcases.ChannelFlow.CF_LevelSetMovementTest(2, lsEvo, lsHandl, tsScheme);
        //    using (var solver = new XNSE_SolverMain()) {
        //        solver.Init(C);
        //        solver.RunSolverMode();

        //        double[] BmQ_RB = solver.ComputeBenchmarkQuantities_RisingBubble();

        //        double err_thrsld = 1e-4;

        //        // x-position
        //        double err = Math.Abs(0.6 - BmQ_RB[0]);
        //        Assert.Less(err, err_thrsld, "error in x-position too high");
        //        Console.WriteLine("error in x-position = {0}", err);

        //        // y-position
        //        err = Math.Abs(0.5 - BmQ_RB[1]);
        //        Assert.Less(err, err_thrsld, "error in y-position too high");
        //        Console.WriteLine("error in y-position = {0}", err);

        //        // circularity
        //        err = Math.Abs(1.0 - BmQ_RB[2]);
        //        Assert.Less(err, err_thrsld, "error in circularity too high");
        //        Console.WriteLine("error in circularity = {0}", err);

        //        // x-velocity
        //        err = Math.Abs(1.0 - BmQ_RB[3]);
        //        Assert.Less(err, err_thrsld, "error in x-velocity too high");
        //        Console.WriteLine("error in x-velocity = {0}", err);

        //        // y-velocity
        //        err = Math.Abs(BmQ_RB[4]);
        //        Assert.Less(err, err_thrsld, "error in y-velocity too high");
        //        Console.WriteLine("error in y-velocity = {0}", err);

        //    }

        //}

        //[Test]
        //public static void CircleMovementTest_WithSurfaceTension(
        //    [Values(LevelSetEvolution.FastMarching, LevelSetEvolution.ExtensionVelocity)]  LevelSetEvolution lsEvo,
        //    [Values(LevelSetHandling.LieSplitting, LevelSetHandling.Coupled_Once, LevelSetHandling.Coupled_Iterative)] LevelSetHandling lsHandl,
        //    [Values(XNSE_Control.TimesteppingScheme.ImplicitEuler, XNSE_Control.TimesteppingScheme.CrankNicolson, XNSE_Control.TimesteppingScheme.BDF2)] XNSE_Control.TimesteppingScheme tsScheme) {

        //    var C = PhysicalBasedTestcases.ChannelFlow.CF_LevelSetMovementTest(2, lsEvo, lsHandl, tsScheme);

        //    double sigma = 1.0;
        //    C.PhysicalParameters.Sigma = sigma;
        //    double Pjump = sigma / 0.25;
        //    C.InitialValues_Evaluators.Add("Pressure#A", X => Pjump);

        //}


        // ============================
        // Check surface tension forces
        // ============================


    }

}
