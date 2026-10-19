"""Exercise classification and atomic merging against real isolated Git histories."""
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]


def module(name):
    spec = importlib.util.spec_from_file_location(name, ROOT / 'tools' / (name + '.py'))
    loaded = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(loaded)
    return loaded


policy_tool = module('upstream_policy')
merge_tool = module('merge_routine_upstream')


class UpstreamPolicyTests(unittest.TestCase):
    def setUp(self):
        parent = ROOT / 'obj' / 'policy-tests'
        parent.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=parent)
        self.folder = Path(self.temp.name).resolve()
        assert self.folder.is_relative_to(parent.resolve())
        self.addCleanup(self.temp.cleanup)
        self.old_cwd = Path.cwd()
        os.chdir(self.folder)
        self.addCleanup(os.chdir, self.old_cwd)
        self.git('init', '-b', 'master')
        self.git('config', 'user.name', 'Policy tests')
        self.git('config', 'user.email', 'tests@example.invalid')
        self.write('docs/upstream-note.md', 'first\nsecond\nthird\nfourth\n')
        self.write('README.md', 'upstream readme\n')
        self.git('add', '.')
        self.git('commit', '-m', 'Shared history')
        self.shared = self.git('rev-parse', 'HEAD')
        self.git('branch', 'upstream')
        self.write('WrathCombo/Configuration/HotbarConfig.cs', 'fork auto-close\n')
        self.write('README.md', 'fork installation instructions\n')
        self.git('add', '.')
        self.git('commit', '-m', 'Fork features')
        self.base = self.git('rev-parse', 'HEAD')

    def git(self, *args):
        result = subprocess.run(['git', *args], text=True, capture_output=True, check=True)
        return result.stdout.strip()

    def write(self, path, content):
        file = self.folder / path
        file.parent.mkdir(parents=True, exist_ok=True)
        file.write_text(content, encoding='utf-8')

    def propose(self, path, content):
        self.git('switch', '-C', 'upstream', self.shared)
        self.write(path, content)
        self.git('add', '.')
        self.git('commit', '-m', 'Upstream change')
        self.upstream = self.git('rev-parse', 'HEAD')
        self.git('switch', '--detach', self.base)
        self.git('merge', '--no-edit', self.upstream)
        return policy_tool.classify(self.base, self.upstream, 'HEAD')

    def test_routine_condition_and_documentation(self):
        for path in ['docs/upstream-note.md', 'docs/usage.md']:
            with self.subTest(path=path):
                result = self.propose(path, 'routine adjustment\n')
                self.assertTrue(result['routine'], result['reasons'])

    def test_dependencies_publishing_configuration_and_unknown_files_need_review(self):
        for path in ['.github/workflows/release.yml', '.gitmodules', 'global.json',
                     'NuGet.config', 'Directory.Build.props', 'WrathCombo/ExpandedHotbars.csproj',
                     'WrathCombo/packages.lock.json', 'PunishLib/source.cs',
                     'WrathCombo/Nodes/NewNode.cs', 'WrathCombo/Configuration/NewConfig.cs',
                     'WrathCombo/Conditions/ConditionBase.cs', 'tools/new_check.py',
                     'pluginmaster.json', 'assets/icons/icon.png', 'LICENSE.md', 'new-unknown-file.cs']:
            with self.subTest(path=path):
                result = self.propose(path, 'upstream update\n')
                self.assertFalse(result['routine'], path)

    def test_fork_modified_files_need_review_even_when_allowlisted(self):
        self.git('switch', 'master')
        self.write('docs/upstream-note.md', 'first\nsecond\nthird\nfork change\n')
        self.git('add', '.')
        self.git('commit', '-m', 'Fork now customises a condition')
        self.base = self.git('rev-parse', 'HEAD')
        result = self.propose('docs/upstream-note.md', 'upstream first\nsecond\nthird\nfourth\n')
        self.assertFalse(result['routine'])
        self.assertTrue(any('modified by the fork' in reason for reason in result['reasons']))

    def test_references_to_fork_hooks_or_native_interop_need_review(self):
        for content in ['CloseAfterUse', 'OnKeybindPressed', 'unsafe void Execute()', '[DllImport("x")]']:
            result = self.propose('docs/upstream-note.md', content + '\n')
            self.assertFalse(result['routine'], content)

    def test_additional_update_branch_edits_are_not_treated_as_upstream(self):
        result = self.propose('docs/usage.md', 'routine docs\n')
        self.assertTrue(result['routine'])
        self.write('docs/extra.md', 'unexpected branch edit\n')
        self.git('add', '.')
        self.git('commit', '-m', 'Extra update-branch edit')
        result = policy_tool.classify(self.base, self.upstream, 'HEAD')
        self.assertFalse(result['routine'])
        self.assertTrue(any('differs from a clean merge' in reason for reason in result['reasons']))

    def test_removed_condition_needs_review(self):
        self.git('switch', 'upstream')
        self.git('rm', 'docs/upstream-note.md')
        self.git('commit', '-m', 'Remove saved condition type')
        upstream = self.git('rev-parse', 'HEAD')
        self.git('switch', '--detach', self.base)
        self.git('merge', '--no-edit', upstream)
        self.assertFalse(policy_tool.classify(self.base, upstream, 'HEAD')['routine'])

    def test_merge_conflict_needs_manual_review(self):
        self.git('switch', 'upstream')
        self.write('README.md', 'conflicting upstream installation\n')
        self.git('add', '.')
        self.git('commit', '-m', 'Conflict with fork readme')
        upstream = self.git('rev-parse', 'HEAD')
        result = policy_tool.classify(self.base, upstream, upstream)
        self.assertFalse(result['routine'])
        self.assertTrue(any('differs from a clean merge' in reason for reason in result['reasons']))

    def test_submodule_with_routine_looking_name_needs_review(self):
        self.git('switch', 'upstream')
        self.git('update-index', '--add', '--cacheinfo', f'160000,{self.shared},docs/library.md')
        self.git('commit', '-m', 'Add dependency disguised as documentation')
        upstream = self.git('rev-parse', 'HEAD')
        self.git('switch', '--detach', self.base)
        self.git('merge', '--no-edit', upstream)
        self.assertFalse(policy_tool.classify(self.base, upstream, 'HEAD')['routine'])

    def prepare_remote(self, policy):
        remote_temp = tempfile.TemporaryDirectory(dir=self.folder.parent)
        remote = Path(remote_temp.name).resolve()
        assert remote.is_relative_to(self.folder.parent)
        self.addCleanup(remote_temp.cleanup)
        self.git('init', '--bare', str(remote))
        self.git('remote', 'add', 'origin', str(remote))
        self.git('push', 'origin', f'{policy["base"]}:refs/heads/master',
                 f'{policy["head"]}:refs/heads/codex/upstream-sync')
        info = {'state': 'OPEN', 'isDraft': False, 'isCrossRepository': False,
                'headRefOid': policy['head'], 'baseRefOid': policy['base'],
                'headRefName': 'codex/upstream-sync', 'baseRefName': 'master',
                'author': {'login': 'app/github-actions', 'is_bot': True}}
        real_output = merge_tool.output

        def with_fake_github(*args):
            return json.dumps(info) if args[0] == 'gh' else real_output(*args)
        return remote, info, with_fake_github

    def test_successful_merge_pushes_exact_validated_commit(self):
        policy = self.propose('docs/usage.md', 'routine docs\n')
        remote, _, fake = self.prepare_remote(policy)
        with patch.object(merge_tool, 'output', side_effect=fake):
            self.assertTrue(merge_tool.merge(policy, 'master', 'codex/upstream-sync', 'test-pr'))
        self.assertEqual(self.git('--git-dir', str(remote), 'rev-parse', 'master'), policy['head'])

    def test_manual_policy_does_not_even_contact_github(self):
        with patch.object(merge_tool, 'output', side_effect=AssertionError('Must not attempt a merge')):
            self.assertFalse(merge_tool.merge({'routine': False}, 'master', 'codex/upstream-sync', 'test-pr'))

    def test_changed_default_branch_blocks_merge(self):
        policy = self.propose('docs/usage.md', 'routine docs\n')
        remote, _, fake = self.prepare_remote(policy)
        self.git('switch', '--detach', self.base)
        self.write('new-default.txt', 'new fork commit\n')
        self.git('add', '.')
        self.git('commit', '-m', 'Default changed while checks ran')
        newer = self.git('rev-parse', 'HEAD')
        self.git('push', 'origin', f'{newer}:refs/heads/master')
        self.git('switch', '--detach', policy['head'])
        with patch.object(merge_tool, 'output', side_effect=fake):
            with self.assertRaisesRegex(ValueError, 'changed while checks ran'):
                merge_tool.merge(policy, 'master', 'codex/upstream-sync', 'test-pr')
        self.assertEqual(self.git('--git-dir', str(remote), 'rev-parse', 'master'), newer)

    def test_changed_pr_identity_blocks_merge(self):
        policy = self.propose('docs/usage.md', 'routine docs\n')
        remote, info, fake = self.prepare_remote(policy)
        info['author']['login'] = 'another-author'
        with patch.object(merge_tool, 'output', side_effect=fake):
            with self.assertRaisesRegex(ValueError, 'identity or commit changed'):
                merge_tool.merge(policy, 'master', 'codex/upstream-sync', 'test-pr')
        self.assertEqual(self.git('--git-dir', str(remote), 'rev-parse', 'master'), policy['base'])

    def test_changed_update_branch_blocks_merge(self):
        policy = self.propose('docs/usage.md', 'routine docs\n')
        remote, _, fake = self.prepare_remote(policy)
        self.write('docs/later.md', 'branch changed after checks\n')
        self.git('add', '.')
        self.git('commit', '-m', 'Another update branch push')
        self.git('push', 'origin', 'HEAD:refs/heads/codex/upstream-sync')
        self.git('switch', '--detach', policy['head'])
        with patch.object(merge_tool, 'output', side_effect=fake):
            with self.assertRaisesRegex(ValueError, 'changed while checks ran'):
                merge_tool.merge(policy, 'master', 'codex/upstream-sync', 'test-pr')
        self.assertEqual(self.git('--git-dir', str(remote), 'rev-parse', 'master'), policy['base'])

    def test_concurrent_push_after_last_check_is_rejected_atomically(self):
        policy = self.propose('docs/usage.md', 'routine docs\n')
        remote, _, fake = self.prepare_remote(policy)
        self.git('switch', '--detach', self.base)
        self.write('race.txt', 'concurrent fork change\n')
        self.git('add', '.')
        self.git('commit', '-m', 'Concurrent fork commit')
        newer = self.git('rev-parse', 'HEAD')
        self.git('switch', '--detach', policy['head'])

        def race_after_pr_check(*args):
            result = fake(*args)
            if args[0] == 'gh':
                self.git('push', 'origin', f'{newer}:refs/heads/master')
            return result
        with patch.object(merge_tool, 'output', side_effect=race_after_pr_check):
            with self.assertRaises(subprocess.CalledProcessError):
                merge_tool.merge(policy, 'master', 'codex/upstream-sync', 'test-pr')
        self.assertEqual(self.git('--git-dir', str(remote), 'rev-parse', 'master'), newer)


if __name__ == '__main__':
    unittest.main()
