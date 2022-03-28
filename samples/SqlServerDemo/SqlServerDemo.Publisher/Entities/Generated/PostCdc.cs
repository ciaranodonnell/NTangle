/*
 * This file is automatically generated; any changes will be lost. 
 */

#nullable enable
#pragma warning disable

using CoreEx.Entities;
using NTangle;
using NTangle.Cdc;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SqlServerDemo.Publisher.Entities
{
    /// <summary>
    /// Represents the CDC model for the root (parent) database table '[Legacy].[Posts]'.
    /// </summary>
    public partial class PostCdc : IEntity
    {
        /// <summary>
        /// Gets or sets the Posts Id '[Legacy].[Posts].[PostsId]' column value.
        /// </summary>
        [JsonPropertyName("postsId")]
        public int PostsId { get; set; }

        /// <summary>
        /// Gets or sets the Text '[Legacy].[Posts].[Text]' column value.
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the Date '[Legacy].[Posts].[Date]' column value.
        /// </summary>
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        /// <summary>
        /// Gets or sets the related (one-to-many) <see cref="PostCdc.CommentCollection"/> (database table '[Legacy].[Comments]').
        /// </summary>
        [JsonPropertyName("comments")]
        public PostCdc.CommentCdcCollection? Comments { get; set; }

        /// <summary>
        /// Gets or sets the related (one-to-many) <see cref="PostCdc.PostsTagsCollection"/> (database table '[Legacy].[Tags]').
        /// </summary>
        [JsonPropertyName("tags")]
        public PostCdc.PostsTagsCdcCollection? Tags { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("etag")]
        public string? ETag { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public CompositeKey PrimaryKey => new CompositeKey(PostsId);

        #region CommentCdc

        /// <summary>
        /// Represents the CDC model for the related (child) database table '[Legacy].[Comments]' (known uniquely as 'Comments').
        /// </summary>
        public partial class CommentCdc : IPrimaryKey
        {
            /// <summary>
            /// Gets or sets the Comments Id '[Legacy].[Comments].[CommentsId]' column value.
            /// </summary>
            [JsonPropertyName("commentsId")]
            public int CommentsId { get; set; }

            /// <summary>
            /// Gets or sets the Posts Id '[Legacy].[Comments].[PostsId]' column value. This column is used within the join.
            /// </summary>
            [JsonIgnore]
            public int PostsId { get; set; }

            /// <summary>
            /// Gets or sets the Text '[Legacy].[Comments].[Text]' column value.
            /// </summary>
            [JsonPropertyName("text")]
            public string? Text { get; set; }

            /// <summary>
            /// Gets or sets the Date '[Legacy].[Comments].[Date]' column value.
            /// </summary>
            [JsonPropertyName("date")]
            public DateTime? Date { get; set; }

            /// <summary>
            /// Gets or sets the related (one-to-many) <see cref="PostCdc.CommentsTagsCollection"/> (database table '[Legacy].[Tags]').
            /// </summary>
            [JsonPropertyName("tags")]
            public PostCdc.CommentsTagsCdcCollection? Tags { get; set; }

            /// <inheritdoc/>
            [JsonIgnore]
            public CompositeKey PrimaryKey => new CompositeKey(CommentsId);
        }

        /// <summary>
        /// Represents the CDC model collection for the related (child) database table '[Legacy].[Comments]'.
        /// </summary>
        public partial class CommentCdcCollection : List<CommentCdc> { }

        #endregion

        #region CommentsTagsCdc

        /// <summary>
        /// Represents the CDC model for the related (child) database table '[Legacy].[Tags]' (known uniquely as 'CommentsTags').
        /// </summary>
        public partial class CommentsTagsCdc : IPrimaryKey
        {
            /// <summary>
            /// Gets or sets the Tags Id '[Legacy].[CommentsTags].[TagsId]' column value.
            /// </summary>
            [JsonPropertyName("tagsId")]
            public int TagsId { get; set; }

            /// <summary>
            /// Gets or sets the Parent Type '[Legacy].[CommentsTags].[ParentType]' column value. This column is used within the join.
            /// </summary>
            [JsonIgnore]
            public string? ParentType { get; set; }

            /// <summary>
            /// Gets or sets the Parent Id '[Legacy].[CommentsTags].[ParentId]' column value. This column is used within the join.
            /// </summary>
            [JsonIgnore]
            public int ParentId { get; set; }

            /// <summary>
            /// Gets or sets the Text '[Legacy].[CommentsTags].[Text]' column value.
            /// </summary>
            [JsonPropertyName("text")]
            public string? Text { get; set; }

            /// <inheritdoc/>
            [JsonIgnore]
            public CompositeKey PrimaryKey => new CompositeKey(TagsId);

            /// <summary>
            /// Gets or sets the 'Posts_PostsId' additional joining column (informational); for internal join use only (not serialized).
            /// </summary>
            [JsonIgnore]
            public int Posts_PostsId { get; set; }
        }

        /// <summary>
        /// Represents the CDC model collection for the related (child) database table '[Legacy].[CommentsTags]'.
        /// </summary>
        public partial class CommentsTagsCdcCollection : List<CommentsTagsCdc> { }

        #endregion

        #region PostsTagsCdc

        /// <summary>
        /// Represents the CDC model for the related (child) database table '[Legacy].[Tags]' (known uniquely as 'PostsTags').
        /// </summary>
        public partial class PostsTagsCdc : IPrimaryKey
        {
            /// <summary>
            /// Gets or sets the Tags Id '[Legacy].[PostsTags].[TagsId]' column value.
            /// </summary>
            [JsonPropertyName("tagsId")]
            public int TagsId { get; set; }

            /// <summary>
            /// Gets or sets the Parent Type '[Legacy].[PostsTags].[ParentType]' column value. This column is used within the join.
            /// </summary>
            [JsonIgnore]
            public string? ParentType { get; set; }

            /// <summary>
            /// Gets or sets the Posts Id '[Legacy].[PostsTags].[ParentId]' column value. This column is used within the join.
            /// </summary>
            [JsonIgnore]
            public int PostsId { get; set; }

            /// <summary>
            /// Gets or sets the Text '[Legacy].[PostsTags].[Text]' column value.
            /// </summary>
            [JsonPropertyName("text")]
            public string? Text { get; set; }

            /// <inheritdoc/>
            [JsonIgnore]
            public CompositeKey PrimaryKey => new CompositeKey(TagsId);
        }

        /// <summary>
        /// Represents the CDC model collection for the related (child) database table '[Legacy].[PostsTags]'.
        /// </summary>
        public partial class PostsTagsCdcCollection : List<PostsTagsCdc> { }

        #endregion
    }
}

#pragma warning restore
#nullable restore