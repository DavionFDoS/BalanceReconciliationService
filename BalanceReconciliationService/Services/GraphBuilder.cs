namespace BalanceReconciliationService.Services
{
    /// <summary>
    /// Class that builds graph as incidence matrix
    /// </summary>
    public class GraphBuilder
    {
        // Список вершин
        private readonly List<Vertex> vertexList;

        private readonly IList<FlowData> _flowsData;
        public GraphBuilder(IList<FlowData> flowsData)
        {
            ArgumentNullException.ThrowIfNull(flowsData, nameof(flowsData));
            _flowsData = flowsData;
            vertexList = new List<Vertex>();
        }

        private void GetVertexList()
        {
            for (int i = 0; i < _flowsData.Count; i++)
            {
                var sourceId = _flowsData[i].SourceId;
                var destinationId = _flowsData[i].DestinationId;

                if (sourceId != "null")
                {
                    bool isExisted = false;

                    if (vertexList.Count == 0)
                    {
                        vertexList.Add(new Vertex(sourceId));
                        isExisted = true;
                    }

                    foreach (Vertex vertex in vertexList)
                    {
                        if (sourceId.Equals(vertex.Id))
                        {
                            isExisted = true;
                            break;
                        }
                    }

                    if (!isExisted)
                    {
                        vertexList.Add(new Vertex(sourceId));
                    }
                }
                else if (destinationId != "null")
                {
                    bool isExisted = false;

                    if (vertexList.Count == 0)
                    {
                        vertexList.Add(new Vertex(destinationId));
                        isExisted = true;
                    }

                    foreach (Vertex vertex in vertexList)
                    {
                        if (destinationId.Equals(vertex.Id))
                        {
                            isExisted = true;
                            break;
                        }
                    }

                    if (!isExisted && vertexList.Count != 0)
                    {
                        vertexList.Add(new Vertex(destinationId));
                    }
                }
            }
            Log.Information("VertexList has been recieved");
        }

        public double[,] GetIncidenceMatrix()
        {
            GetVertexList();
            var incidenceMatrix = new double[vertexList.Count, _flowsData.Count];

            for (var flow = 0; flow < _flowsData.Count; flow++)
            {
                string sourceId = _flowsData[flow].SourceId;
                string destinationId = _flowsData[flow].DestinationId;

                for (int vertex = 0; vertex < vertexList.Count; vertex++)
                {
                    if (destinationId.Equals(vertexList[vertex].Id))
                    {
                        incidenceMatrix[vertex, flow] = 1;
                    }
                    if (sourceId.Equals(vertexList[vertex].Id))
                    {
                        incidenceMatrix[vertex, flow] = -1;
                    }
                }
            }

            Log.Information("Incidence matrix has been recieved");

            return incidenceMatrix;
        }
    }
}
