﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.Serialization;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using NUnit.Framework;
using SaaS.Wires;

namespace Sample.CQRS.Portable
{
    public class FileDocumentReaderWriterTest : DocumentReaderWriterTest
    {
        [SetUp]
        public void Setup()
        {
            var tmpPath = Path.GetTempPath();
            var documentStrategy = new DocumentStrategy();
            _reader = new FileDocumentReaderWriter<Guid, int>(tmpPath, documentStrategy);
            _writer = new FileDocumentReaderWriter<Guid, int>(tmpPath, documentStrategy);
            _testClassReader = new FileDocumentReaderWriter<unit, Test1>(tmpPath, documentStrategy);
            _testClassWtiter = new FileDocumentReaderWriter<unit, Test1>(tmpPath, documentStrategy);
            _guidKeyClassReader = new FileDocumentReaderWriter<Guid, Test1>(tmpPath, documentStrategy);
            _guidKeyClassWriter = new FileDocumentReaderWriter<Guid, Test1>(tmpPath, documentStrategy);
        }
    }

    public class MemoryDocumentReaderWriterTest : DocumentReaderWriterTest
    {
        [SetUp]
        public void Setup()
        {
            var documentStrategy = new DocumentStrategy();
            var concurrentDictionary = new ConcurrentDictionary<string, byte[]>();
            _reader = new MemoryDocumentReaderWriter<Guid, int>(documentStrategy, concurrentDictionary);
            _writer = new MemoryDocumentReaderWriter<Guid, int>(documentStrategy, concurrentDictionary);
            _testClassReader = new MemoryDocumentReaderWriter<unit, Test1>(documentStrategy, concurrentDictionary);
            _testClassWtiter = new MemoryDocumentReaderWriter<unit, Test1>(documentStrategy, concurrentDictionary);
            _guidKeyClassReader = new MemoryDocumentReaderWriter<Guid, Test1>(documentStrategy, concurrentDictionary);
            _guidKeyClassWriter = new MemoryDocumentReaderWriter<Guid, Test1>(documentStrategy, concurrentDictionary);
        }
    }

    public abstract class DocumentReaderWriterTest
    {
        public IDocumentReader<Guid, int> _reader;
        public IDocumentWriter<Guid, int> _writer;
        public IDocumentReader<unit, Test1> _testClassReader;
        public IDocumentWriter<unit, Test1> _testClassWtiter;
        public IDocumentReader<Guid, Test1> _guidKeyClassReader;
        public IDocumentWriter<Guid, Test1> _guidKeyClassWriter;

        [Test]
        public void get_not_created_entity()
        {
            //GIVEN
            var key = Guid.NewGuid();
            int entity;

            //WHEN
            Assert.AreEqual(false, _reader.TryGet(key, out entity));
        }

        [Test]
        public void deleted_not_created_entity()
        {
            //GIVEN
            var key = Guid.NewGuid();

            //WHEN
            Assert.AreEqual(false, _writer.TryDelete(key));
        }

        [Test]
        public void created_new_entity()
        {
            //GIVEN
            var key = Guid.NewGuid();

            //WHEN
            Assert.AreEqual(1, _writer.AddOrUpdate(key, () => 1, i => 5, AddOrUpdateHint.ProbablyDoesNotExist));
        }

        [Test]
        public void update_exist_entity()
        {
            //GIVEN
            var key = Guid.NewGuid();
            _writer.AddOrUpdate(key, () => 1, i => 5, AddOrUpdateHint.ProbablyDoesNotExist);

            //WHEN
            Assert.AreEqual(5, _writer.AddOrUpdate(key, () => 1, i => 5, AddOrUpdateHint.ProbablyDoesNotExist));
        }

        [Test]
        public void get_new_created_entity()
        {
            //GIVEN
            var key = Guid.NewGuid();
            _writer.AddOrUpdate(key, () => 1, i => 5, AddOrUpdateHint.ProbablyDoesNotExist);

            //WHEN
            int result;
            Assert.AreEqual(true, _reader.TryGet(key, out result));
            Assert.AreEqual(1, result);
        }

        [Test]
        public void get_updated_entity()
        {
            //GIVEN
            var key = Guid.NewGuid();
            _writer.AddOrUpdate(key, () => 1, i => 2, AddOrUpdateHint.ProbablyDoesNotExist);
            _writer.AddOrUpdate(key, () => 3, i => 4, AddOrUpdateHint.ProbablyDoesNotExist);

            //WHEN
            int result;
            Assert.AreEqual(true, _reader.TryGet(key, out result));
            Assert.AreEqual(4, result);
        }

        [Test]
        public void get_by_key_does_not_exist()
        {
            //GIVEN
            var result = _reader.Get(Guid.NewGuid());

            //WHEN
            Assert.IsFalse(result.HasValue);
        }

        [Test]
        public void get_by_exis_key()
        {
            //GIVEN
            var key = Guid.NewGuid();
            _writer.AddOrUpdate(key, () => 1, i => 2, AddOrUpdateHint.ProbablyDoesNotExist);
            var result = _reader.Get(key);

            //WHEN
            Assert.IsTrue(result.HasValue);
            Assert.AreEqual(1, result.Value);
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void load_by_key_does_not_exist()
        {
            //GIVEN
            _reader.Load(Guid.NewGuid());
        }

        [Test]
        public void load_by_exis_key()
        {
            //GIVEN
            var key = Guid.NewGuid();
            _writer.AddOrUpdate(key, () => 1, i => 2, AddOrUpdateHint.ProbablyDoesNotExist);
            var result = _reader.Load(key);

            //WHEN
            Assert.AreEqual(1, result);
        }

        [Test, Ignore("to be realized ExtendDocumentReader.GetOrNew")]
        public void get_or_new()
        {

        }

        [Test, Ignore("to be realized ExtendDocumentReader.Get<TSingleton>")]
        public void get_singleton()
        {

        }

        [Test]
        public void when_not_found_key_get_new_view_and_not_call_action()
        {
            //GIVEN
            Test1 t = new Test1 { Value = 555 };
            var key = Guid.NewGuid();
            var newValue = _guidKeyClassWriter.AddOrUpdate(key, t, tv => { tv.Value += 1; });

            //WHEN
            Assert.AreEqual(t, newValue);
            Assert.AreEqual(555, newValue.Value);
        }

        [Test]
        public void when_key_exist_call_action_and_get_new_value()
        {
            //GIVEN
            Test1 t = new Test1 { Value = 555 };
            var key = Guid.NewGuid();
            _guidKeyClassWriter.AddOrUpdate(key, t, tv => { tv.Value += 1; });
            var newValue = _guidKeyClassWriter.AddOrUpdate(key, t, tv => { tv.Value += 1; });

            //WHEN
            Assert.AreEqual(typeof(Test1), newValue.GetType());
            Assert.AreEqual(556, newValue.Value);
        }

        [Test]
        public void when_not_found_key_get_new_view_func_and_not_call_action()
        {
            //GIVEN
            Test1 t = new Test1 { Value = 555 };
            var key = Guid.NewGuid();
            var newValue = _guidKeyClassWriter.AddOrUpdate(key, () => t, tv => { tv.Value += 1; });

            //WHEN
            Assert.AreEqual(t, newValue);
            Assert.AreEqual(555, newValue.Value);
        }

        [Test]
        public void when_key_exist_not_call_new_view_func_and_call_action_and_get_new_value()
        {
            //GIVEN
            Test1 t = new Test1 { Value = 555 };
            var key = Guid.NewGuid();
            _guidKeyClassWriter.AddOrUpdate(key, t, tv => { tv.Value += 1; });
            var newValue = _guidKeyClassWriter.AddOrUpdate(key, () => t, tv => { tv.Value += 1; });

            //WHEN
            Assert.AreEqual(typeof(Test1), newValue.GetType());
            Assert.AreEqual(556, newValue.Value);
        }

        [Test]
        public void add_new_value()
        {
            Test1 t = new Test1 { Value = 555 };
            var newValue = _guidKeyClassWriter.Add(Guid.NewGuid(), t);

            Assert.AreEqual(t, newValue);
            Assert.AreEqual(555, newValue.Value);
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void add_value_when_key_exist()
        {
            Test1 t = new Test1 { Value = 555 };
            var key = Guid.NewGuid();
            _guidKeyClassWriter.Add(key, t);
            _guidKeyClassWriter.Add(key, t);
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void update_value_with_func_when_key_not_found()
        {
            _guidKeyClassWriter.UpdateOrThrow(Guid.NewGuid(), tv =>
                {
                    tv.Value += 1;
                    return tv;
                });
        }

        [Test]
        public void update_value_with_func_when_key_exist()
        {
            Test1 t = new Test1 { Value = 555 };
            var key = Guid.NewGuid();
            _guidKeyClassWriter.Add(key, t);
            var newValue = _guidKeyClassWriter.UpdateOrThrow(key, tv =>
                 {
                     tv.Value += 1;
                     return tv;
                 });


            Assert.AreEqual(556, newValue.Value);
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void update_value_with_action_when_key_not_found()
        {
            _guidKeyClassWriter.UpdateOrThrow(Guid.NewGuid(), tv =>
            {
                tv.Value += 1;
            });
        }

        [Test]
        public void update_value_with_action_when_key_exist()
        {
            Test1 t = new Test1 { Value = 555 };
            var key = Guid.NewGuid();
            _guidKeyClassWriter.Add(key, t);
            var newValue = _guidKeyClassWriter.UpdateOrThrow(key, tv =>
            {
                tv.Value += 1;
            });


            Assert.AreEqual(556, newValue.Value);
        }

        [Test]
        public void created_new_instance_when_call_update_method_and_key_not_found()
        {
            var key = Guid.NewGuid();
            var newValue = _guidKeyClassWriter.UpdateEnforcingNew(key, tv => { tv.Value += 1; });

            var defaultValue = default(int);
            Assert.AreEqual(defaultValue + 1, newValue.Value);
        }
    }


    [DataContract]
    public class Test1
    {
        [DataMember(Order = 1)]
        public int Value { get; set; }
    }

}