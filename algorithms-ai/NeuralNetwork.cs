public class NeuralNetwork
{
    private float[][] weights;
    private float[][] biases;
    private int[] layerSizes;

    public float learningRate;

    // Cache
    private float[][] layerOutputs;
    private float[][] layerInputs;

    public NeuralNetwork(int[] layers, float learningRate = 0.01f)
    {
        this.layerSizes = layers;
        this.learningRate = learningRate;

        weights = new float[layers.Length - 1][];
        biases = new float[layers.Length - 1][];
        layerOutputs = new float[layers.Length][];
        layerInputs = new float[layers.Length][];

        Random rand = new Random();
        for (int i = 0; i < layers.Length - 1; i++)
        {
            int inputSize = layers[i];
            int outputSize = layers[i + 1];

            weights[i] = new float[inputSize * outputSize];
            biases[i] = new float[outputSize];

            // Xavier initialization con float
            float scale = (float)Math.Sqrt(2.0f / inputSize);
            for (int j = 0; j < weights[i].Length; j++)
                weights[i][j] = ((float)rand.NextDouble() * 2 - 1) * scale;
        }

        for (int i = 0; i < layers.Length; i++)
        {
            layerOutputs[i] = new float[layers[i]];
            layerInputs[i] = new float[layers[i]];
        }
    }

    // Funzioni di attivazione ottimizzate per float
    private float Tanh(float x) => (float)Math.Tanh(x);
    private float Sigmoid(float x) => 1.0f / (1.0f + (float)Math.Exp(-x));
    private float ReLU(float x) => x > 0 ? x : 0;

    public float[] Predict(float[] inputs)
    {
        Array.Copy(inputs, layerOutputs[0], inputs.Length);

        for (int layer = 0; layer < weights.Length; layer++)
        {
            int inputSize = layerSizes[layer];
            int outputSize = layerSizes[layer + 1];
            bool isOutputLayer = (layer == weights.Length - 1);

            for (int o = 0; o < outputSize; o++)
            {
                float sum = biases[layer][o];
                for (int i = 0; i < inputSize; i++)
                    sum += layerOutputs[layer][i] * weights[layer][i * outputSize + o];

                layerInputs[layer + 1][o] = sum;

                if (isOutputLayer)
                    layerOutputs[layer + 1][o] = Tanh(sum);
                else
                    layerOutputs[layer + 1][o] = ReLU(sum);
            }
        }

        return layerOutputs[^1];
    }

    public void Train(float[] inputs, float[] targets)
    {
        Predict(inputs);

        float[][] deltas = new float[weights.Length][];
        for (int i = 0; i < deltas.Length; i++)
            deltas[i] = new float[layerSizes[i + 1]];

        // Output layer (Sigmoid)
        int lastLayer = weights.Length - 1;
        for (int i = 0; i < layerSizes[lastLayer + 1]; i++)
        {
            float output = layerOutputs[lastLayer + 1][i];
            float error = targets[i] - output;
            deltas[lastLayer][i] = error * output * (1 - output);
        }

        // Hidden layers (ReLU)
        for (int layer = lastLayer - 1; layer >= 0; layer--)
        {
            int currentSize = layerSizes[layer + 1];
            int nextSize = layerSizes[layer + 2];

            for (int i = 0; i < currentSize; i++)
            {
                float error = 0;
                for (int j = 0; j < nextSize; j++)
                    error += deltas[layer + 1][j] * weights[layer + 1][i * nextSize + j];

                float input = layerInputs[layer + 1][i];
                // Derivata di ReLU: 1 se input > 0, altrimenti 0
                deltas[layer][i] = error * (input > 0 ? 1.0f : 0.0f);
            }
        }

        // Update con float
        for (int layer = 0; layer < weights.Length; layer++)
        {
            int inputSize = layerSizes[layer];
            int outputSize = layerSizes[layer + 1];

            for (int i = 0; i < inputSize; i++)
            {
                for (int o = 0; o < outputSize; o++)
                {
                    int idx = i * outputSize + o;
                    weights[layer][idx] += learningRate * deltas[layer][o] * layerOutputs[layer][i];
                }
            }

            for (int o = 0; o < outputSize; o++)
                biases[layer][o] += learningRate * deltas[layer][o];
        }
    }
}