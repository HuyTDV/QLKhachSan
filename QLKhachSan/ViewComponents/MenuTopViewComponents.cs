using Microsoft.AspNetCore.Mvc;
using QLKhachSan.Models;

namespace QLKhachSan.ViewComponents
{
    public class MenuTopViewComponent : ViewComponent
    {
        private readonly Hotel01Context _context;


        public MenuTopViewComponent(Hotel01Context context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var items = _context.Menus.Where(m => (bool)m.IsActive).OrderBy(m => m.SortOrder).ToList();
            return await Task.FromResult<IViewComponentResult>(View(items));
        }
    }
}
