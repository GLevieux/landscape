using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;
using GeneticSharp.Infrastructure.Framework.Texts;
using GeneticSharp.Infrastructure.Framework.Commons;

namespace GeneticSharp.Domain.Mutations
{
    public class CustomMutation : MutationBase
    {
        private int[] m_mutableGenesIndexes;
        private readonly bool m_allGenesMutable;

        public CustomMutation(params int[] mutableGenesIndexes)
        {
            m_mutableGenesIndexes = mutableGenesIndexes;
        }

        public CustomMutation(bool allGenesMutable)
        {
            m_allGenesMutable = allGenesMutable;
        }

        public CustomMutation() : this(false)
        {
        }

        protected override void PerformMutate(IChromosome chromosome, float probability)
        {
            ExceptionHelper.ThrowIfNull("chromosome", chromosome);

            var genesLength = chromosome.Length;

            if (m_mutableGenesIndexes == null || m_mutableGenesIndexes.Length == 0)
            {
                if (m_allGenesMutable)
                {
                    m_mutableGenesIndexes = Enumerable.Range(0, genesLength).ToArray();
                }
                else
                {
                    m_mutableGenesIndexes = RandomizationProvider.Current.GetInts(1, 0, genesLength);
                }
            }

            for (int i = 0; i < m_mutableGenesIndexes.Length; i++)
            {
                var geneIndex = m_mutableGenesIndexes[i];

                if (geneIndex >= genesLength)
                {
                    throw new MutationException(this, "The chromosome has no gene on index {0}. The chromosome genes length is {1}.".With(geneIndex, genesLength));
                }

                if (RandomizationProvider.Current.GetDouble() <= probability)
                {
                    chromosome.ReplaceGene(geneIndex, chromosome.GenerateGene(geneIndex));
                }
            }
        }
    }
}