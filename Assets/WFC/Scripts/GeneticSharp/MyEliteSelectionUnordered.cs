using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Randomizations;

namespace GeneticSharp.Domain.Selections
{
    /// <summary>
    /// Selects the chromosomes with the best fitness.
    /// </summary>
    /// <remarks>
    /// Also know as: Truncation Selection.
    /// </remarks>    
    [DisplayName("Elite")]
    public sealed class MyEliteSelectionUnordered : SelectionBase
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneticSharp.Domain.Selections.EliteSelection"/> class.
        /// </summary>
        public MyEliteSelectionUnordered() : base(2)
        {
        }
        #endregion

        #region ISelection implementation
        /// <summary>
        /// Performs the selection of chromosomes from the generation specified.
        /// </summary>
        /// <param name="number">The number of chromosomes to select.</param>
        /// <param name="generation">The generation where the selection will be made.</param>
        /// <returns>The select chromosomes.</returns>
        protected override IList<IChromosome> PerformSelectChromosomes(int number, Generation generation)
        {
            var ordered = generation.Chromosomes.OrderByDescending(c => c.Fitness);
            var selected = ordered.Take(number);

            var randomNumbers = selected.Select(r => RandomizationProvider.Current.GetInt(0,int.MaxValue)).ToArray();
            var orderedResult = selected.Zip(randomNumbers, (r, o) => new { Result = r, Order = o })
                .OrderBy(o => o.Order)
                .Select(o => o.Result);
            
            return selected.ToList();
        }

        #endregion
    }
}