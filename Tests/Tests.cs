using Xunit;
using Newtonsoft.Json;
using BalanceReconciliationService.Models;
using BalanceReconciliationService.Services;
using BalanceReconciliationService.Interfaces;
using System.Collections.Generic;
using System.IO;
using System;

namespace Tests
{
    public class Tests
    {
        const string fileName = @".\NewModel.json";       
        private readonly MeasuredInputs? measuredInputs;
        private readonly MatrixDataPreparer dataPreparer;
        private readonly ISolver solver;

        public Tests()
        {
            measuredInputs = JsonConvert.DeserializeObject<MeasuredInputs>(File.ReadAllText(fileName));
            dataPreparer = new MatrixDataPreparer(measuredInputs);
            solver = new AccordSolver(dataPreparer);
        }
        
        [Fact]
        public void ReconcileBalance()
        {
            var expectedSolution = new[] { 10.055612418500504, 3.0144745895183522, 7.041137828982151, 1.9822547563048074, 5.058883072677343, 4.067257698582969, 0.9916253740943739};
            List<double> actual = new();

            var reconsiledOutputs = solver.Solve();
            foreach (var reconciledFlowData in reconsiledOutputs.ReconciledFlowDatas)
            {
                actual.Add(reconciledFlowData.ReconciledValue);
            }

            Assert.Equal(expectedSolution, actual.ToArray());
        }

        [Fact]
        public void CalculateDisbalance()
        {
            var expectedMeasuredDisbalance = 0.2879496483762398;
            var expectedReconciledDisbalance = 9.155133597044475E-16;

            var reconsiledOutput = solver.Solve();

            Assert.Equal(expectedMeasuredDisbalance, reconsiledOutput.MeasuredDataDisbalance);
            Assert.Equal(expectedReconciledDisbalance, reconsiledOutput.ReconciledDataDisbalance);
        }

        [Fact]
        public void CheckDataPreparationCorrectness()
        {
            var expectedUpperTechnologicalBounds = new[] { 1000.0, 1000.0, 1000.0, 1000.0, 1000.0, 1000.0, 1000.0 };
            var expectedLowerTechnologicalBounds = new[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
            var expectedUpperMetrologicalBounds = new[] { 10.205, 3.155, 7.514, 2.025, 5.195, 4.138, 1.011 };
            var expectedLowerMetrologicalBounds = new[] { 9.805, 2.912, 6.148, 1.945, 4.991, 3.976, 0.971 };
            var expectedMeasuredData = new[] { 10.005, 3.033, 6.831, 1.985, 5.093, 4.057 , 0.991 };
           
            Assert.Equal(expectedUpperTechnologicalBounds, dataPreparer.UpperTechnologicalBound.ToArray());
            Assert.Equal(expectedLowerTechnologicalBounds, dataPreparer.LowerTechnologicalBound.ToArray());
            Assert.Equal(expectedUpperMetrologicalBounds, dataPreparer.UpperMetrologicalBound.ToArray());
            Assert.Equal(expectedLowerMetrologicalBounds, dataPreparer.LowerMetrologicalBound.ToArray());
            Assert.Equal(expectedMeasuredData, dataPreparer.MeasuredValues.ToArray());
        }
    }
}