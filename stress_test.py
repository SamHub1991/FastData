#!/usr/bin/env python3
"""
FastData ORM 压力测试脚本

功能：
- 测试各种数据库操作的并发性能
- 支持多数据库测试（SqlServer, MySql, PostgreSql）
- 生成详细的性能报告

使用方法：
python stress_test.py [--host HOST] [--db DB] [--threads THREADS] [--iterations ITERATIONS]
"""

import argparse
import requests
import time
import threading
import json
from collections import defaultdict
from datetime import datetime
import statistics

class StressTest:
    def __init__(self, host, db_keys, threads, iterations):
        self.host = host
        self.db_keys = db_keys
        self.threads = threads
        self.iterations = iterations
        self.results = defaultdict(list)
        self.lock = threading.Lock()
        
    def test_endpoint(self, endpoint, method='GET', data=None, db_key='SqlServer'):
        """测试单个端点"""
        url = f"{self.host}/api/ConcurrencyTest/{endpoint}"
        params = {'dbKey': db_key} if db_key else {}
        
        start_time = time.time()
        try:
            if method == 'GET':
                response = requests.get(url, params=params, timeout=10)
            elif method == 'POST':
                response = requests.post(url, params=params, json=data, timeout=10)
            elif method == 'PUT':
                response = requests.put(url, params=params, timeout=10)
            elif method == 'DELETE':
                response = requests.delete(url, params=params, timeout=10)
            
            elapsed = (time.time() - start_time) * 1000  # 转换为毫秒
            
            success = response.status_code == 200
            result = {
                'endpoint': endpoint,
                'success': success,
                'elapsed_ms': elapsed,
                'status_code': response.status_code,
                'db_key': db_key
            }
            
            if success:
                try:
                    json_response = response.json()
                    result['response'] = json_response
                except:
                    result['response'] = None
            else:
                result['error'] = response.text[:200]
            
            return result
        except Exception as e:
            elapsed = (time.time() - start_time) * 1000
            return {
                'endpoint': endpoint,
                'success': False,
                'elapsed_ms': elapsed,
                'error': str(e),
                'db_key': db_key
            }
    
    def worker(self, test_name, endpoint, method, data, db_key):
        """工作线程"""
        for i in range(self.iterations):
            result = self.test_endpoint(endpoint, method, data, db_key)
            with self.lock:
                self.results[test_name].append(result)
    
    def run_tests(self):
        """运行所有测试"""
        print("=" * 80)
        print(f"FastData ORM 压力测试")
        print(f"测试时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print(f"测试参数: 线程数={self.threads}, 迭代次数={self.iterations}")
        print("=" * 80)
        
        # 定义测试场景
        test_scenarios = [
            # 单条查询测试
            ('单条查询', 'query/single', 'GET', None),
            # 批量查询测试
            ('批量查询', 'query/batch', 'GET', None),
            # 分页查询测试
            ('分页查询', 'query/paged', 'GET', None),
            # 聚合查询测试
            ('聚合查询', 'query/aggregate', 'GET', None),
            # 插入测试
            ('插入操作', 'write/insert', 'POST', None),
            # 更新测试
            ('更新操作', 'write/update', 'PUT', None),
            # 删除测试
            ('删除操作', 'write/delete', 'DELETE', None),
            # 事务测试
            ('事务操作', 'transaction', 'POST', None),
        ]
        
        # 对每个数据库执行测试
        for db_key in self.db_keys:
            print(f"\n开始测试数据库: {db_key}")
            print("-" * 80)
            
            # 重置统计数据
            self.test_endpoint('reset', 'POST')
            
            for test_name, endpoint, method, data in test_scenarios:
                print(f"\n[{test_name}] 测试开始...")
                
                threads = []
                for i in range(self.threads):
                    t = threading.Thread(
                        target=self.worker,
                        args=(f"{test_name}_{db_key}", endpoint, method, data, db_key)
                    )
                    threads.append(t)
                    t.start()
                
                for t in threads:
                    t.join()
                
                print(f"[{test_name}] 测试完成")
        
        # 混合读写压力测试
        print("\n[混合读写压力测试] 测试开始...")
        for db_key in self.db_keys:
            threads = []
            for i in range(self.threads):
                t = threading.Thread(
                    target=self.worker,
                    args=(f"混合读写_{db_key}", 'mixed/stress', 'POST', None, db_key)
                )
                threads.append(t)
                t.start()
            
            for t in threads:
                t.join()
        
        print("[混合读写压力测试] 测试完成")
        
        # 获取统计数据
        print("\n获取服务器统计数据...")
        stats_response = self.test_endpoint('stats', 'GET')
        if stats_response['success']:
            print("统计数据获取成功")
        
        self.print_report()
    
    def print_report(self):
        """打印测试报告"""
        print("\n" + "=" * 80)
        print("压力测试报告")
        print("=" * 80)
        
        for test_name, results in sorted(self.results.items()):
            if not results:
                continue
            
            success_count = sum(1 for r in results if r['success'])
            failure_count = len(results) - success_count
            elapsed_times = [r['elapsed_ms'] for r in results]
            
            avg_elapsed = statistics.mean(elapsed_times)
            min_elapsed = min(elapsed_times)
            max_elapsed = max(elapsed_times)
            
            if len(elapsed_times) >= 2:
                stdev_elapsed = statistics.stdev(elapsed_times)
            else:
                stdev_elapsed = 0
            
            p50 = statistics.median(elapsed_times)
            if len(elapsed_times) >= 10:
                sorted_times = sorted(elapsed_times)
                p95 = sorted_times[int(len(sorted_times) * 0.95)]
                p99 = sorted_times[int(len(sorted_times) * 0.99)]
            else:
                p95 = max_elapsed
                p99 = max_elapsed
            
            print(f"\n[{test_name}]")
            print(f"  总请求数: {len(results)}")
            print(f"  成功数: {success_count} ({success_count * 100.0 / len(results):.2f}%)")
            print(f"  失败数: {failure_count} ({failure_count * 100.0 / len(results):.2f}%)")
            print(f"  平均延迟: {avg_elapsed:.2f} ms")
            print(f"  最小延迟: {min_elapsed:.2f} ms")
            print(f"  最大延迟: {max_elapsed:.2f} ms")
            print(f"  标准差: {stdev_elapsed:.2f} ms")
            print(f"  P50 延迟: {p50:.2f} ms")
            print(f"  P95 延迟: {p95:.2f} ms")
            print(f"  P99 延迟: {p99:.2f} ms")
            
            # 显示错误信息（如果有）
            errors = [r['error'] for r in results if not r['success'] and 'error' in r]
            if errors:
                print(f"  错误示例: {errors[0][:100]}")
        
        print("\n" + "=" * 80)
        print("测试完成")
        print("=" * 80)

def main():
    parser = argparse.ArgumentParser(description='FastData ORM 压力测试')
    parser.add_argument('--host', default='http://localhost:5000', help='API 服务地址')
    parser.add_argument('--db', nargs='+', default=['SqlServer'], 
                        choices=['SqlServer', 'MySql', 'PostgreSql'],
                        help='测试的数据库类型')
    parser.add_argument('--threads', type=int, default=10, help='并发线程数')
    parser.add_argument('--iterations', type=int, default=10, help='每个线程的迭代次数')
    
    args = parser.parse_args()
    
    tester = StressTest(args.host, args.db, args.threads, args.iterations)
    tester.run_tests()

if __name__ == '__main__':
    main()