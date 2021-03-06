﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoSSS.Solution.XdgTimestepping;
using BoSSS.Solution.Multigrid;

namespace BoSSS.Application.IBM_Solver {
    class SolverChooser {

        /// <summary>
        /// Choose solver depending on configurations made in the control file.
        /// </summary>
        /// <param name="nonlinSol"></param>
        /// <param name="linSol"></param>
        /// <param name="Timestepper"></param>
        public static void ChooseSolver(IBM_Control Control, ref XdgBDFTimestepping Timestepper) {

            // Set several solver options for Timestepper
            Timestepper.Config_SolverConvergenceCriterion = Control.Solver_ConvergenceCriterion;
            Timestepper.Config_MaxIterations = Control.MaxSolverIterations;
            Timestepper.Config_MinIterations = Control.MinSolverIterations;
            Timestepper.Config_MaxKrylovDim = Control.MaxKrylovDim;

            // Set nonlinear Solver
            switch (Control.NonlinearSolve) {
                case NonlinearSolverCodes.NewtonGMRES:
                    Timestepper.Config_NonlinearSolver = NonlinearSolverMethod.Newton;
                    break;
                case NonlinearSolverCodes.Picard:
                    Timestepper.Config_NonlinearSolver = NonlinearSolverMethod.Picard;
                    break;
                default:
                    throw new NotImplementedException("Nonlinear solver option not available");
            }


            switch (Control.LinearSolve) {
                case LinearSolverCodes.automatic:
                    AutomaticChoice(Control,Timestepper);
                    break;

                case LinearSolverCodes.classic_mumps:
                    Timestepper.Config_linearSolver = new DirectSolver() { WhichSolver = DirectSolver._whichSolver.MUMPS };
                    break;

                case LinearSolverCodes.classic_pardiso:
                    Timestepper.Config_linearSolver = new DirectSolver() { WhichSolver = DirectSolver._whichSolver.PARDISO };
                    break;

                case LinearSolverCodes.exp_schwarz_directcoarse_overlap:

                    if (Control.NoOfMultigridLevels < 2)
                        throw new ApplicationException("At least 2 Multigridlevels are required");

                    Timestepper.Config_linearSolver = new Schwarz() {
                        m_BlockingStrategy = new Schwarz.METISBlockingStrategy() {
                            NoOfPartsPerProcess = 1,
                        },
                        Overlap = 1,
                        CoarseSolver = DetermineMGSquence(Control.NoOfMultigridLevels - 2)
                    };
                    break;

                case LinearSolverCodes.exp_schwarz_directcoarse:

                    if (Control.NoOfMultigridLevels < 2)
                        throw new ApplicationException("At least 2 Multigridlevels are required");

                    Timestepper.Config_linearSolver = new Schwarz() {
                        m_BlockingStrategy = new Schwarz.METISBlockingStrategy() {
                            NoOfPartsPerProcess = 1,
                        },
                        Overlap = 0,
                        CoarseSolver = DetermineMGSquence(Control.NoOfMultigridLevels - 2)
                    };
                    break;

                case LinearSolverCodes.exp_schwarz_Kcycle_directcoarse:

                    if (Control.NoOfMultigridLevels < 2)
                        throw new ApplicationException("At least 2 Multigridlevels are required");

                    Timestepper.Config_linearSolver = new Schwarz() {
                        m_BlockingStrategy = new Schwarz.MultigridBlocks() {
                            Depth = Control.NoOfMultigridLevels - 1
                        },
                        Overlap = 0,
                        CoarseSolver = DetermineMGSquence(Control.NoOfMultigridLevels - 2)
                    };
                    break;

                case LinearSolverCodes.exp_schwarz_Kcycle_directcoarse_overlap:

                    if (Control.NoOfMultigridLevels < 2)
                        throw new ApplicationException("At least 2 Multigridlevels are required");

                    Timestepper.Config_linearSolver = new Schwarz() {
                        m_BlockingStrategy = new Schwarz.MultigridBlocks() {
                            Depth = Control.NoOfMultigridLevels - 1
                        },
                        Overlap = 1,
                        CoarseSolver = DetermineMGSquence(Control.NoOfMultigridLevels - 2)
                    };
                    break;

                case LinearSolverCodes.exp_softgmres:
                    Timestepper.Config_linearSolver = new SoftGMRES() {
                        MaxKrylovDim = Timestepper.Config_MaxKrylovDim,
                    };
                    break;

                case LinearSolverCodes.exp_softgmres_schwarz_Kcycle_directcoarse_overlap:
                    Timestepper.Config_linearSolver = new SoftGMRES() {
                        MaxKrylovDim = Timestepper.Config_MaxKrylovDim, 
                        Precond = new Schwarz() {
                            m_BlockingStrategy = new Schwarz.MultigridBlocks() {
                                Depth = Control.NoOfMultigridLevels - 1
                            },
                            Overlap = 1,
                            CoarseSolver = DetermineMGSquence(Control.NoOfMultigridLevels - 2)
                        },
                    };
                    break;

                default:
                    throw new NotImplementedException("Linear solver option not available");
            }
        }

        /// <summary>
        /// Automatic choice of linear solver depending on problem size, immersed boundary, polynomial degree, etc.
        /// </summary>
        static void AutomaticChoice(IBM_Control Control, XdgBDFTimestepping Timestepper) {
            throw new NotImplementedException("Option currently not available");

            // Detecting MPI Size
            MPI.Wrappers.csMPI.Raw.Comm_Size(MPI.Wrappers.csMPI.Raw._COMM.WORLD, out int size);
            //Timestepper.MultigridSequence[0].

            // Spatial Dimension

            //Timestepper.

            // Wenn Gesamtproblem in 2D < 100000 DoFs -> Direct Solver
            // Wenn Gesamtproblem in 3D < 10000 DoFs -> Direct Solver 

            // Block Solve 3D ca. 6000 DoFs per Process -> Adjust Blocks per Process
            // Coarse Solve ca. 5000 bis 10000 DoFs. -> Adjust Multigrid Levels

        }

        /// <summary>
        /// Determines a solver sequence depending on MGlevels
        /// </summary>
        /// <param name="MGlevels"></param>
        /// <param name="CoarsestSolver"></param>
        /// <returns></returns>
        static ISolverSmootherTemplate DetermineMGSquence(int MGlevels) {
            ISolverSmootherTemplate solver;
            if (MGlevels > 0) {
                solver = new ClassicMultigrid() { CoarserLevelSolver = DetermineMGSquence(MGlevels - 1) };
            } else {
                solver = new DirectSolver() { WhichSolver = DirectSolver._whichSolver.MUMPS };
            }
            return solver;
        }
    }
}
