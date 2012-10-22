﻿#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data
{
  //todo: check whether it is possible and better to rename 'Movie' to 'MovieInfo' for consistency within the TheMovieDb-API-Lib
  /// <summary>
  /// Contains all available information about a specific <see cref="AbstractMovie"/>.
  /// http://help.themoviedb.org/kb/api/movie-info
  /// </summary>
  /// <example>
  /// {
  ///   "adult": false,
  ///   "backdrop_path": "/mOTtuakUTb1qY6jG6lzMfjdhLwc.jpg",
  ///   "belongs_to_collection": {
  ///     "backdrop_path": "/mOTtuakUTb1qY6jG6lzMfjdhLwc.jpg",
  ///     "id": 10,
  ///     "name": "Star Wars Collection",
  ///     "poster_path": "/6rddZZpxMQkGlpQYVVxb2LdQRI3.jpg"
  ///   },
  ///   "budget": 11000000,
  ///   "genres": [
  ///     {
  ///       "id": 28,
  ///       "name": "Action"
  ///     },
  ///     {
  ///       "id": 14,
  ///       "name": "Fantasy"
  ///     },
  ///     {
  ///       "id": 878,
  ///       "name": "Science Fiction"
  ///     }
  ///   ],
  ///   "homepage": "http://www.starwars.com",
  ///   "id": 11,
  ///   "imdb_id": "tt0076759",
  ///   "original_title": "Star Wars: Episode IV: A New Hope",
  ///   "overview": "Princess Leia is captured and held hostage by the evil Imperial forces in their effort to take over the galactic Empire. Venturesome Luke Skywalker and dashing captain Han Solo team together with the loveable robot duo R2-D2 and C-3PO to rescue the beautiful princess and restore peace and justice in the Empire.",
  ///   "popularity": 84.8,
  ///   "poster_path": "/qoETrQ73Jbd2LDN8EUfNgUerhzG.jpg",
  ///   "production_companies": [
  ///     {
  ///       "id": 1,
  ///       "name": "Lucasfilm"
  ///     },
  ///     {
  ///       "id": 8265,
  ///       "name": "Paramount"
  ///     }
  ///   ],
  ///   "production_countries": [
  ///     {
  ///       "iso_3166_1": "TN",
  ///       "name": "Tunisia"
  ///     },
  ///     {
  ///       "iso_3166_1": "US",
  ///       "name": "United States of America"
  ///     }
  ///   ],
  ///   "release_date": "1977-12-27",
  ///   "revenue": 775398007,
  ///   "runtime": 121,
  ///   "spoken_languages": [
  ///     {
  ///       "iso_639_1": "en",
  ///       "name": "English"
  ///     }
  ///   ],
  ///   "tagline": "A long time ago in a galaxy far, far away...",
  ///   "title": "Star Wars: Episode IV: A New Hope",
  ///   "vote_average": 8.8,
  ///   "vote_count": 75
  /// }
  /// </example>
  [DataContract]
  public class Movie : MovieSearchResult
  {
    [DataMember(Name = "imdb_id")]
    public string ImdbId { get; set; }

    [DataMember(Name = "overview")]
    public string Overview { get; set; }

    [DataMember(Name = "tagline")]
    public string Tagline { get; set; }

    [DataMember(Name = "revenue")]
    public long? Revenue { get; set; }

    [DataMember(Name = "budget")]
    public long? Budget { get; set; }

    [DataMember(Name = "runtime")]
    public int? Runtime { get; set; }

    [DataMember(Name = "homepage")]
    public string Homepage { get; set; }

    [DataMember(Name = "status")]
    public string Status { get; set; }

    [DataMember(Name = "belongs_to_collection")]
    public MovieCollection Collection { get; set; }

    [DataMember(Name = "genres")]
    public List<Genre> Genres { get; set; }

    [DataMember(Name = "production_companies")]
    public List<Company> ProductionCompanies { get; set; }

    #region ProductionCountry class

    /// <remarks>
    ///   {
    ///   "iso_3166_1": "DE",
    ///   "name": "Germany"
    ///   },
    /// </remarks>
    [DataContract]
    public class ProductionCountry
    {
      [DataMember(Name = "iso_3166_1")]
      public string CountryCode { get; set; }

      [DataMember(Name = "name")]
      public string Name { get; set; }

      public override string ToString()
      {
        return Name;
      }
    }

    #endregion

    [DataMember(Name = "production_countries")]
    public List<ProductionCountry> ProductionCountries { get; set; }

    #region SpokenLanguage class

    /// <remarks>
    ///   {
    ///   "iso_639_1": "en",
    ///   "name": "English"
    ///   }
    /// </remarks>
    [DataContract]
    public class SpokenLanguage
    {
      [DataMember(Name = "iso_639_1")]
      public string Id { get; set; }

      [DataMember(Name = "name")]
      public string Name { get; set; }

      public override string ToString()
      {
        return Name;
      }
    }

    #endregion

    [DataMember(Name = "spoken_languages")]
    public List<SpokenLanguage> SpokenLanguages { get; set; }
  }
}