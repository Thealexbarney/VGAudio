using System;
using System.IO;
using System.Linq;
using static DspAdpcm.Helpers;

namespace DspAdpcm
{
    internal static class InterleaveExtensions
    {
        public static T[] Interleave<T>(this T[][] inputs, int interleaveSize, int paddingAlignment = 0)
        {
            int length = inputs[0].Length;
            if (inputs.Any(x => x.Length != length))
                throw new ArgumentOutOfRangeException(nameof(inputs), "Inputs must be of equal length");

            int numInputs = inputs.Length;
            int numBlocks = length.DivideByRoundUp(interleaveSize);
            int lastInterleaveSize = length - (numBlocks - 1) * interleaveSize;
            int padding = GetNextMultiple(lastInterleaveSize, paddingAlignment) - lastInterleaveSize;

            var output = new T[(interleaveSize * (numBlocks - 1) + lastInterleaveSize + padding) * numInputs];

            for (int b = 0; b < numBlocks; b++)
            {
                for (int i = 0; i < numInputs; i++)
                {
                    int currentInterleaveSize = b == numBlocks - 1 ? lastInterleaveSize : interleaveSize;
                    Array.Copy(inputs[i], interleaveSize * b,
                        output, interleaveSize * b * numInputs + currentInterleaveSize * i,
                        currentInterleaveSize);
                }
            }

            return output;
        }

        public static void Interleave(this byte[][] inputs, Stream output, int length, int interleaveSize, int paddingAlignment = 0)
        {
            if (inputs.Any(x => x.Length < length))
                throw new ArgumentOutOfRangeException(nameof(inputs), "Inputs must be as long as the specified length");

            int numInputs = inputs.Length;
            int numBlocks = length.DivideByRoundUp(interleaveSize);
            int lastInterleaveSize = length - (numBlocks - 1) * interleaveSize;
            int padding = GetNextMultiple(lastInterleaveSize, paddingAlignment) - lastInterleaveSize;

            for (int b = 0; b < numBlocks; b++)
            {
                for (int o = 0; o < numInputs; o++)
                {
                    output.Write(inputs[o], interleaveSize * b, b != numBlocks - 1 ? interleaveSize : lastInterleaveSize);
                    if (b == numBlocks - 1)
                    {
                        output.Position += padding;
                    }
                }
            }
        }

        public static T[][] DeInterleave<T>(this T[] input, int interleaveSize, int numOutputs, int outputSize = -1)
        {
            if (input.Length % numOutputs != 0)
                throw new ArgumentOutOfRangeException(nameof(numOutputs), numOutputs,
                    $"The input array length ({input.Length}) must be divisible by the number of outputs.");

            int inputSize = input.Length / numOutputs;
            if (outputSize == -1)
                outputSize = inputSize;
            
            int numBlocks = inputSize.DivideByRoundUp(interleaveSize);
            int lastInputInterleaveSize = inputSize - (numBlocks - 1) * interleaveSize;
            int lastOutputInterleaveSize = outputSize - (numBlocks - 1) * interleaveSize;
            lastOutputInterleaveSize = Math.Min(lastOutputInterleaveSize, lastInputInterleaveSize);

            var outputs = new T[numOutputs][];
            for (int i = 0; i < numOutputs; i++)
            {
                outputs[i] = new T[outputSize];
            }

            for (int b = 0; b < numBlocks; b++)
            {
                for (int o = 0; o < numOutputs; o++)
                {
                    int currentInputInterleaveSize = b == numBlocks - 1 ? lastInputInterleaveSize : interleaveSize;
                    int currentOutputInterleaveSize = b == numBlocks - 1 ? lastOutputInterleaveSize : interleaveSize;

                    Array.Copy(input, interleaveSize * b * numOutputs + currentInputInterleaveSize * o,
                        outputs[o], interleaveSize * b,
                        currentOutputInterleaveSize);
                }
            }

            return outputs;
        }

        public static byte[][] DeInterleave(this Stream input, int length, int interleaveSize, int numOutputs, int outputSize = -1)
        {
            if (input.CanSeek)
            {
                long remainingLength = input.Length - input.Position;
                if (remainingLength < length)
                {
                    throw new ArgumentOutOfRangeException(nameof(length), length,
                        "Specified length is less than the number of bytes remaining in the Stream");
                }
            }

            if (length % numOutputs != 0)
                throw new ArgumentOutOfRangeException(nameof(numOutputs), numOutputs,
                    $"The input length ({length}) must be divisible by the number of outputs.");

            int inputSize = length / numOutputs;
            if (outputSize == -1)
                outputSize = inputSize;
            
            int numBlocks = inputSize.DivideByRoundUp(interleaveSize);
            int lastInputInterleaveSize = inputSize - (numBlocks - 1) * interleaveSize;
            int lastOutputInterleaveSize = outputSize - (numBlocks - 1) * interleaveSize;
            lastOutputInterleaveSize = Math.Min(lastOutputInterleaveSize, lastInputInterleaveSize);

            var outputs = new byte[numOutputs][];
            for (int i = 0; i < numOutputs; i++)
            {
                outputs[i] = new byte[outputSize];
            }

            for (int b = 0; b < (numBlocks - 1); b++)
            {
                for (int o = 0; o < numOutputs; o++)
                {
                    input.Read(outputs[o], interleaveSize * b, interleaveSize);
                }
            }

            for (int o = 0; o < numOutputs; o++)
            {
                input.Read(outputs[o], interleaveSize * (numBlocks - 1), lastOutputInterleaveSize);
                input.Position += lastInputInterleaveSize - lastOutputInterleaveSize;
            }

            return outputs;
        }
    }
}
