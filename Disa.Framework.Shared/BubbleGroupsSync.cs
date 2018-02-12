﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    //FIXME: If BubbleGroupIndex gets re-generated, a lazy group will become a permanent group.
    //FIXME: If a lazy bubble group gets merged into a unified bubble group, the lazy tag should be dropped.
    //FIXME: Incoming bubble (event, process, and sync), drop lazy tag
    //TODO: Ensure BubbleGroupUpdater does not update Lazy groups
    public class BubbleGroupsSync
    {
        public interface Agent
        {
            Task<IEnumerable<BubbleGroup>> LoadBubbleGroups(IEnumerable<Tag> tags = null);

            Task<bool> OnLazyBubbleGroupsDeleted(List<BubbleGroup> groups);
        }

        private static void SortListByTime(List<BubbleGroup> list)
        {
            list.TimSort((x, y) =>
            {
                var timeX = x.LastBubbleSafe().Time;
                var timeY = y.LastBubbleSafe().Time;
                return -timeX.CompareTo(timeY);
            });
        }

        public class Cursor : IEnumerable<BubbleGroup>, IDisposable
        {
            private readonly List<Tag> _tags;
            
            public Cursor(IEnumerable<Tag> tags)
            {
                _tags = tags.ToList();
            }
            
            private IEnumerable<BubbleGroup> LoadBubblesInternalLazyService()
            {
                var tagServices = _tags.Select(t => t.Service).ToHashSet();
                
                var serviceBubbleGroupsEnumerators = tagServices.Select(service =>
                {
                    var agent = service as Agent;
                    var serviceTags = _tags.Where(t => t.Service == service).ToList();
                    if (agent != null)
                    {
                        // Service supports lazy loading
                        var task = agent.LoadBubbleGroups(serviceTags);
                        try
                        {
                            task.Wait();
                        }
                        catch (Exception ex)
                        {
                            Utils.DebugPrint($"{service} threw exception: {ex}");
                            return TagManager.GetAllBubbleGroups(serviceTags)
                                             .OrderByDescending(g => g.LastBubbleSafe().Time)
                                             .ToList();
                        }
                        return task.Result;
                    }
                    else
                    {
                        // Service does not support lazy loading
                        // Get the bubble groups from Tag Manager
                        return TagManager.GetAllBubbleGroups(serviceTags)
                                         .OrderByDescending(g => g.LastBubbleSafe().Time)
                                         .ToList();
                    }
                });

                return Utils.LazySorting(serviceBubbleGroupsEnumerators, group => group.LastBubbleSafe().Time);
            }

            public IEnumerator<BubbleGroup> GetEnumerator()
            {
                //return LoadBubblesInternalService().GetEnumerator();
                return LoadBubblesInternalLazyService().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            //private class RemoveDuplicatesComparer : IEqualityComparer<BubbleGroup>
            //{
            //    public bool Equals(BubbleGroup x, BubbleGroup y)
            //    {
            //        if (x.Service != y.Service)
            //            return false;

            //        return x.Service.BubbleGroupComparer(x.Address, y.Address);
            //    }

            //    public int GetHashCode(BubbleGroup obj)
            //    {
            //        return 0;
            //    }
            //}

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            
            ~Cursor()
            {
                Dispose(false);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposing)
                {
                    return;
                }
                
                //FIXME: ensure services are running of the deleted groups
                var lazys = BubbleGroupManager.FindAll(x => x.Lazy);
                foreach (var lazy in lazys)
                {
                    BubbleGroupFactory.Delete(lazy, false);
                }

                //FIXME: call into service needs to be atomic.
                foreach (var lazyGroup in lazys.GroupBy(x => x.Service))
                {
                    var key = lazyGroup.Key;
                    var agent = key as Agent;
                    agent.OnLazyBubbleGroupsDeleted(lazyGroup.ToList());
                }
            }
        }
        
        public class TagBasedCursor : IEnumerable<BubbleGroup>
        {
            private readonly List<Tag> _tags;
            
            public TagBasedCursor(IEnumerable<Tag> tags)
            {
                _tags = tags.ToList();
            }
            
            private IEnumerable<BubbleGroup> LoadBubblesInternalTagManager()
            {
                var bubbleGroups = TagManager.GetAllBubbleGroups(_tags);
                return bubbleGroups.OrderByDescending(g => g.LastBubbleSafe().Time);
            }
            
            public IEnumerator<BubbleGroup> GetEnumerator()
            {
                return LoadBubblesInternalTagManager().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            
            ~TagBasedCursor()
            {
                Dispose(false);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposing)
                {
                    return;
                }
            }
        }
    }
}
