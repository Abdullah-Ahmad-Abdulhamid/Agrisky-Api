using Agrisky.Models;
using AgriskyApi.IRepo;
using Microsoft.EntityFrameworkCore;

namespace AgriskyApi.Repo
{
    public class GenricRepo<T>:IGenricRepo<T> where T : class 
    {
        protected readonly AppDbcontext _context;
        private readonly DbSet<T> _dbSet;

        public GenricRepo(AppDbcontext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAll()
            => await _dbSet.ToListAsync();

        public async Task<T> GetById(int id)
            => await _dbSet.FindAsync(id);

        public async Task Add(T entity)
            => await _dbSet.AddAsync(entity);

        public void Update(T entity)
            => _dbSet.Update(entity);

        public void Delete(T entity)
            => _dbSet.Remove(entity);

        public async Task SaveAsync()
            => await _context.SaveChangesAsync();
    }
}
