using System;
using System.IO;

#if NET20
using DspAdpcm.Compatibility.LinqBridge;
#else
using System.Linq;
#endif

namespace DspAdpcm.Utilities
{
    internal static class InterleaveExtensions
    {
        public static T[] Interleave<T>(this T[][] inputs, int interleaveSize, int outputSize = -1)
        {
            int inputSize = inputs[0].Length;
            if (outputSize == -1)
                outputSize = inputSize;

            if (inputs.Any(x => x.Length != inputSize))
                throw new ArgumentOutOfRangeException(nameof(inputs), "Inputs must be of equal length");

            int inputCount = inputs.Length;
            int blockCount = outputSize.DivideByRoundUp(interleaveSize);
            int lastInputInterleaveSize = inputSize - (blockCount - 1) * interleaveSize;
            int lastOutputInterleaveSize = outputSize - (blockCount - 1) * interleaveSize;
            lastInputInterleaveSize = Math.Min(lastOutputInterleaveSize, lastInputInterleaveSize);

            var output = new T[outputSize * inputCount];

            for (int b = 0; b < blockCount; b++)
            {
                for (int i = 0; i < inputCount; i++)
                {
                    int currentInputInterleaveSize = b == blockCount - 1 ? lastInputInterleaveSize : interleaveSize;
                    int currentOutputInterleaveSize = b == blockCount - 1 ? lastOutputInterleaveSize : interleaveSize;

                    Array.Copy(inputs[i], interleaveSize * b,
                        output, interleaveSize * b * inputCount + currentOutputInterleaveSize * i,
                        currentInputInterleaveSize);
                }
            }

            return output;
        }

        public static void Interleave(this byte[][] inputs, Stream output, int interleaveSize, int outputSize = -1)
        {
            int inputSize = inputs[0].Length;
            if (outputSize == -1)
                outputSize = inputSize;

            if (inputs.Any(x => x.Length != inputSize))
                throw new ArgumentOutOfRangeException(nameof(inputs), "Inputs must be of equal length");

            int inputCount = inputs.Length;
            int blockCount = outputSize.DivideByRoundUp(interleaveSize);
            int lastInputInterleaveSize = inputSize - (blockCount - 1) * interleaveSize;
            int lastOutputInterleaveSize = outputSize - (blockCount - 1) * interleaveSize;
            lastInputInterleaveSize = Math.Min(lastOutputInterleaveSize, lastInputInterleaveSize);

            for (int b = 0; b < blockCount - 1; b++)
            {
                for (int o = 0; o < inputCount; o++)
                {
                    output.Write(inputs[o], interleaveSize * b, interleaveSize);
                }
            }

            for (int o = 0; o < inputCount; o++)
            {
                output.Write(inputs[o], interleaveSize * (blockCount - 1), lastInputInterleaveSize);
                output.Position += lastOutputInterleaveSize - lastInputInterleaveSize;
            }

            //Simply setting the position past the end of the stream doesn't expand the stream,
            //so we do that manually if necessary
            output.SetLength(Math.Max(output.Position, output.Length));
        }

        public static T[][] DeInterleave<T>(this T[] input, int interleaveSize, int outputCount, int outputSize = -1)
        {
            if (input.Length % outputCount != 0)
                throw new ArgumentOutOfRangeException(nameof(outputCount), outputCount,
                    $"The input array length ({input.Length}) must be divisible by the number of outputs.");

            int inputSize = input.Length / outputCount;
            if (outputSize == -1)
                outputSize = inputSize;

            int blockCount = inputSize.DivideByRoundUp(interleaveSize);
            int lastInputInterleaveSize = inputSize - (blockCount - 1) * interleaveSize;
            int lastOutputInterleaveSize = outputSize - (blockCount - 1) * interleaveSize;
            lastOutputInterleaveSize = Math.Min(lastOutputInterleaveSize, lastInputInterleaveSize);

            var outputs = new T[outputCount][];
            for (int i = 0; i < outputCount; i++)
            {
                outputs[i] = new T[outputSize];
            }

            for (int b = 0; b < blockCount; b++)
            {
                for (int o = 0; o < outputCount; o++)
                {
                    int currentInputInterleaveSize = b == blockCount - 1 ? lastInputInterleaveSize : interleaveSize;
                    int currentOutputInterleaveSize = b == blockCount - 1 ? lastOutputInterleaveSize : interleaveSize;

                    Array.Copy(input, interleaveSize * b * outputCount + currentInputInterleaveSize * o,
                        outputs[o], interleaveSize * b,
                        currentOutputInterleaveSize);
                }
            }

            return outputs;
        }

        public static byte[][] DeInterleave(this Stream input, int length, int interleaveSize, int outputCount, int outputSize = -1)
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

            if (length % outputCount != 0)
                throw new ArgumentOutOfRangeException(nameof(outputCount), outputCount,
                    $"The input length ({length}) must be divisible by the number of outputs.");

            int inputSize = length / outputCount;
            if (outputSize == -1)
                outputSize = inputSize;

            int blockCount = inputSize.DivideByRoundUp(interleaveSize);
            int lastInputInterleaveSize = inputSize - (blockCount - 1) * interleaveSize;
            int lastOutputInterleaveSize = outputSize - (blockCount - 1) * interleaveSize;
            lastOutputInterleaveSize = Math.Min(lastOutputInterleaveSize, lastInputInterleaveSize);

            var outputs = new byte[outputCount][];
            for (int i = 0; i < outputCount; i++)
            {
                outputs[i] = new byte[outputSize];
            }

            for (int b = 0; b < (blockCount - 1); b++)
            {
                for (int o = 0; o < outputCount; o++)
                {
                    input.Read(outputs[o], interleaveSize * b, interleaveSize);
                }
            }

            for (int o = 0; o < outputCount; o++)
            {
                input.Read(outputs[o], interleaveSize * (blockCount - 1), lastOutputInterleaveSize);
                input.Position += lastInputInterleaveSize - lastOutputInterleaveSize;
            }

            return outputs;
        }

        public static byte[] ShortToInterleavedByte( this short[][] input)
        {
            int inputCount = input.Length;
            int length = input[0].Length;
            byte[] output = new byte[inputCount * length * 2];

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < inputCount; j++)
                {
                    int offset = (i * inputCount + j) * 2;
                    output[offset] = (byte)input[j][i];
                    output[offset + 1] = (byte)(input[j][i] >> 8);
                }
            }

            return output;
        }

        public static short[][] InterleavedByteToShort(this byte[] input, int outputCount)
        {
            int itemCount = input.Length / 2 / outputCount;
            short[][] output = new short[outputCount][];
            for (int i = 0; i < outputCount; i++)
            {
                output[i] = new short[itemCount];
            }

            for (int i = 0; i < itemCount; i++)
            {
                for (int o = 0; o < outputCount; o++)
                {
                    int offset = (i * outputCount + o) * 2;
                    output[o][i] = (short)(input[offset] | (input[offset + 1] << 8));
                }
            }

            return output;
        }
    }
}
