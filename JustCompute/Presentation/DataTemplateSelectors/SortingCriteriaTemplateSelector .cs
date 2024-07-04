using JustCompute.Presentation.Models;
using JustCompute.Presentation.Popups.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustCompute.Presentation.DataTemplateSelectors
{
    public class SortingCriteriaTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SelectedTemplate { get; set; }
        public DataTemplate UnselectedTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (item is SelectableSortingCriterion criterion)
            {
                return criterion.IsSelected ? SelectedTemplate : UnselectedTemplate;
            }
            return UnselectedTemplate;
        }
    }
}
