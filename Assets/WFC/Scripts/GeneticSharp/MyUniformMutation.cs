using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;
using GeneticSharp.Infrastructure.Framework.Texts;
using GeneticSharp.Infrastructure.Framework.Commons;


/****
 * 
 * L'opérateur uniforme de mutation de base choisit au départ les indices qu'il veut
 * faire muter. Du coup si on ne met pas tous les gènes mutables, il en choisit un seul
 * au départ et le fait muter ensuite.
 * 
 * Ici, si on choisit de tout muter, c'est le meme comportement, mais si on n'en mute
 * qu'un seul, alors le gene est tiré au sort à chaque fois
 * 
 * */

namespace GeneticSharp.Domain.Mutations
{
    /// <summary>
    /// This operator replaces the value of the chosen gene with a uniform random value selected 
    /// between the user-specified upper and lower bounds for that gene. 
    /// <see href="http://en.wikipedia.org/wiki/Mutation_(genetic_algorithm)">Wikipedia</see>
    /// </summary>
    [DisplayName("Uniform")]
    public class MyUniformMutation : MutationBase
    {
        #region Fields
        private int[] m_mutableGenesIndexes;

        private readonly bool m_allGenesMutable;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneticSharp.Domain.Mutations.UniformMutation"/> class.
        /// </summary>
        /// <param name="mutableGenesIndexes">Mutable genes indexes.</param>
        public MyUniformMutation(params int[] mutableGenesIndexes)
        {
            m_mutableGenesIndexes = mutableGenesIndexes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneticSharp.Domain.Mutations.UniformMutation"/> class.
        /// </summary>
        /// <param name="allGenesMutable">If set to <c>true</c> all genes are mutable.</param>
        public MyUniformMutation(bool allGenesMutable)
        {
            m_allGenesMutable = allGenesMutable;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneticSharp.Domain.Mutations.UniformMutation"/> class.
        /// </summary>
        /// <remarks>Creates an instance of UniformMutation where some random genes will be mutated.</remarks>
        public MyUniformMutation() : this(false)
        {
        }
        #endregion

        #region Methods
        /// <summary>
        /// Mutate the specified chromosome.
        /// </summary>
        /// <param name="chromosome">The chromosome.</param>
        /// <param name="probability">The probability to mutate each chromosome.</param>
        protected override void PerformMutate(IChromosome chromosome, float probability)
        {
            ExceptionHelper.ThrowIfNull("chromosome", chromosome);

            var genesLength = chromosome.Length;

            if (m_mutableGenesIndexes == null || m_mutableGenesIndexes.Length == 0)
            {
                m_mutableGenesIndexes = Enumerable.Range(0, genesLength).ToArray();
            }

            if (m_allGenesMutable)
            {
                for (int i = 0; i < genesLength; i++)
                {
                    if (RandomizationProvider.Current.GetDouble() <= probability)
                    {
                        chromosome.ReplaceGene(i, chromosome.GenerateGene(i));
                    }
                }
            }
            else
            {
                int i = RandomizationProvider.Current.GetInt(0, genesLength);
                if (RandomizationProvider.Current.GetDouble() <= probability)
                {
                    chromosome.ReplaceGene(i, chromosome.GenerateGene(i));
                }
            }
            

            
        }
        #endregion
    }
}