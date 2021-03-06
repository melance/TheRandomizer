﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LiteDB;
using TheRandomizer.Generators;
using TheRandomizer.Utility;
using TheRandomizer.WebApp.Models;
using System.Security.Principal;
using Microsoft.AspNet.Identity;
using TheRandomizer.Generators.Assignment;
using System.IO;

namespace TheRandomizer.WebApp.DataAccess
{
    internal static class DataContext
    {
        private const string DB_PATH = "~/DataAccess/TheRandomizer.db";
        private const string GENERATORS_COLLECTION = "Generators";
        private const string USER_COLLECTION = "Users";
        private const string LIBRARIES_COLLECTION = "Libraries";

        internal static UserModel User
        {
            get
            {
                using (var db = OpenDatabase())
                {
                    var user = db.GetCollection<UserModel>(USER_COLLECTION).FindOne(u => u.Id == HttpContext.Current.User.Identity.GetUserId());
                    if (user == null) user = new UserModel() { Id = HttpContext.Current.User.Identity.GetUserId() };
                    return user;
                }
            }
        }

        public static BsonMapper CreateMapper()
        {
            var mapper = BsonMapper.Global;
            return mapper;
        }

        public static LiteDatabase OpenDatabase()
        {
            var dbPath = HttpContext.Current.Server.MapPath(DB_PATH);
            if (!Directory.Exists(Path.GetDirectoryName(dbPath))) Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
            return new LiteDatabase(dbPath, CreateMapper());
        }
        
        public static BaseGenerator GetGenerator(Guid id)
        {
            using (var db = OpenDatabase())
            {
                var generator = db.GetCollection<BaseGenerator>(GENERATORS_COLLECTION).FindOne(bg => bg.Id == id);
                return generator;
            }
        }        

        public static BaseGenerator GetGenerator(string name)
        {
            using (var db = OpenDatabase())
            {
                var generator = db.GetCollection<BaseGenerator>(GENERATORS_COLLECTION).FindOne(bg => bg.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                return generator;
            }
        }
        
        public static BaseGenerator SetPublished(Guid id, bool published)
        {
            using (var db = OpenDatabase())
            {
                var generator = GetGenerator(id);
                generator.Published = published;
                db.GetCollection<BaseGenerator>(GENERATORS_COLLECTION).Update(generator);
                return generator;
            }
        }

        public static BaseGenerator UpsertGenerator(BaseGenerator generator)
        {
            try
            {
                using (var db = OpenDatabase())
                {
                    var collection = db.GetCollection<BaseGenerator>(GENERATORS_COLLECTION);
                    var user = User;
                    if (collection.FindOne(bg => bg.Name == generator.Name) != null)
                    {
                        collection.Update(generator);
                    }
                    else
                    {
                        var id = collection.Insert(generator);
                        user.OwnerOfGenerator.Add(id.AsGuid);
                        db.GetCollection<UserModel>(USER_COLLECTION).Upsert(user);
                    }
                    return collection.FindOne(bg => bg.Name == generator.Name);
                }
            }
            catch 
            {
                return null;
            }
        }

        public static void DeleteGenerator(Guid id)
        {
            using (var db = OpenDatabase())
            {
                var collection = db.GetCollection<BaseGenerator>(GENERATORS_COLLECTION);
                collection.Delete(g => g.Id == id);
            }
        }

        public static SearchModel Search(SearchModel criteria)
        {
            using (var db = OpenDatabase())
            {
                var tags = criteria.Tags.Where(kvp => kvp.Value).Select(kvp => new Tag(kvp.Key)).ToList();
                var collection = db.GetCollection<BaseGenerator>(GENERATORS_COLLECTION)
                    .FindAll()
                    .Select(bg => bg.AsGeneratorInfo())
                    .OrderBy(bg => bg.Name)
                    .Where(bg => (bg.Tags == null || bg.Tags.Count == 0 || bg.Tags.Intersect(tags).Count() == tags.Count()) 
                                  && (criteria.Name == null || bg.Name.IndexOf(criteria.Name, StringComparison.CurrentCultureIgnoreCase) >= 0)
                                  && (!criteria.FavoritesOnly || User.Favorites.Contains(bg.Id))
                                  && (criteria.Author == null || bg.Author.Equals(criteria.Author, StringComparison.CurrentCultureIgnoreCase))
                                  && (criteria.IncludeLibraries || bg.IsLibrary == false)
                                  && (bg.Published || User.OwnerOfGenerator.Contains(bg.Id)));

                criteria.Results = collection.Select(gi => new Models.SearchResult(gi)).ToList();
            }

            return criteria;
        }

        public static List<GeneratorInfo> GetUnpublished()
        {
            using (var db = OpenDatabase())
            {
                var collection = db.GetCollection<BaseGenerator>(GENERATORS_COLLECTION)
                    .FindAll()
                    .Select(bg => bg.AsGeneratorInfo())
                    .OrderBy(bg => bg.Name)
                    .Where(bg => bg.Published == false)
                    .ToList();

                return collection;
            }
        }

        public static bool SetFavorite(Guid generatorId, bool isFavorite)
        {
            try
            {
                var user = User;
                using (var db = OpenDatabase())
                {
                    if (isFavorite && !user.Favorites.Contains(generatorId))
                    {
                        user.Favorites.Add(generatorId);
                    }
                    else if (!isFavorite && user.Favorites.Contains(generatorId))
                    {
                        user.Favorites.Remove(generatorId);
                    }
                    db.GetCollection<UserModel>(USER_COLLECTION).Upsert(user);
                    return isFavorite;
                }
            }
            catch 
            {
                return !isFavorite;
            }
        }

        public static IDictionary<string, bool> GetAllTags()
        {
            using (var db = OpenDatabase())
            {
                var list = db.GetCollection<BaseGenerator>(GENERATORS_COLLECTION)
                                .FindAll()
                                .SelectMany(bg => bg.Tags)
                                .Distinct()
                                .ToList()
                                .ToDictionary(x => x.Value, x => false);
                return list;
            }
        }

        public static IEnumerable<string> GetAuthors()
        {
            using (var db = OpenDatabase())
            {
                var list = db.GetCollection<BaseGenerator>(GENERATORS_COLLECTION)
                                .FindAll()
                                .Select(bg => bg.Author)
                                .Distinct();
                return list;
            }
        }

        public static IEnumerable<string> GetLibraryNames()
        {
            using (var db = OpenDatabase())
            {
                var list = db.GetCollection<BaseGenerator>(GENERATORS_COLLECTION)
                                .FindAll()
                                .OfType<AssignmentGenerator>()
                                .Where(ag => ag.IsLibrary)
                                .Select(ag => ag.Name);
                return list;
            }
        }
    }
}