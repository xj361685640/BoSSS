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

using BoSSS.Solution;
using BoSSS.Solution.Control;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace CNS.LoadBalancing {

    public class ArtificialViscosityCellCostEstimator : StaticCellCostEstimator {

        public ArtificialViscosityCellCostEstimator(int[] performanceClassToCostMap) : base(performanceClassToCostMap) {
            if (performanceClassToCostMap.Length != 2) {
                throw new ConfigurationException("There are more than two performance classes");
            }
        }

        public static IEnumerable<Func<IApplication<AppControl>, int, ICellCostEstimator>> GetStaticCostBasedEstimator(int[] performanceClassToCostMap) {
            yield return (app, classCount) => new ArtificialViscosityCellCostEstimator(performanceClassToCostMap);
        }

        public static IEnumerable<Func<IApplication<AppControl>, int, ICellCostEstimator>> MultipleBalanceConstraintsFactory(int[] performanceClassToCostMap) {
            yield return (app, classCount) => new ArtificialViscosityCellCostEstimator(performanceClassToCostMap);
            int[] temp = new int[] { performanceClassToCostMap[1], performanceClassToCostMap[0] };
            yield return (app, classCount) => new ArtificialViscosityCellCostEstimator(temp);
        }
    }
}