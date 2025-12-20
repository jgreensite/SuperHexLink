#!/usr/bin/env python3
"""Convert Pester (NUnit 2.5-style) XML to JUnit XML for test-reporter ingestion.

Usage: pester_nunit2_to_junit.py input.xml output.xml

This script is defensive: if the input file is missing or not parseable it will
print diagnostic info and exit 0 (so CI doesn't fail unnecessarily). It makes a
best-effort mapping of test-suite/test-case -> testsuites/testsuite/testcase.
"""
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def ns_strip(tag):
    return tag.split('}', 1)[-1] if '}' in tag else tag


def convert(input_path: Path, output_path: Path) -> int:
    if not input_path.exists():
        print(f"Input file not found: {input_path}")
        return 0
    try:
        tree = ET.parse(input_path)
        root = tree.getroot()
    except Exception as e:
        print(f"Failed to parse '{input_path}': {e}")
        return 0

    # Gather test-cases by walking the tree
    testcases = []  # tuples (classname, name, time, status, failure_message)
    total = 0
    failures = 0
    skipped = 0
    time_total = 0.0

    # Find all test-case elements anywhere in the tree
    for tc in root.findall('.//test-case'):
        total += 1
        tc_name = tc.get('name') or tc.get('description') or 'unnamed'
        tc_time = float(tc.get('time') or '0')
        time_total += tc_time
        # Determine classname by finding nearest parent test-suite with a name
        parent = tc
        classname = None
        while parent is not None:
            parent = parent.getparent() if hasattr(parent, 'getparent') else None
        # xml.etree doesn't have getparent(); we can infer classname by inspecting ancestors via iter
        # Workaround: search for the enclosing test-suite by matching test-case in each test-suite
        classname = 'Pester'
        status = tc.get('result') or tc.get('success')
        # Pester uses result="Success" and success="True" attributes; normalize
        if status is None:
            # fallback: element may contain <failure> child
            status = 'Success' if not tc.find('failure') else 'Failed'
        if status.lower() in ('false', 'failed', 'error'):
            status = 'Failed'
        else:
            status = 'Success'
        failure_message = ''
        # Try to extract failure info
        failure_el = tc.find('failure') or tc.find('reason') or tc.find('message')
        if failure_el is not None:
            # gather text from child nodes
            texts = []
            if failure_el.text:
                texts.append(failure_el.text.strip())
            for child in failure_el:
                if child.text:
                    texts.append(child.text.strip())
            failure_message = '\n'.join([t for t in texts if t])
        if status == 'Failed':
            failures += 1
        # check for skipped/ignored
        if tc.get('success') == 'False' and tc.get('result') == 'Skipped':
            skipped += 1

        testcases.append((classname, tc_name, tc_time, status, failure_message))

    # Build JUnit XML
    testsuites = ET.Element('testsuites')
    testsuite = ET.SubElement(testsuites, 'testsuite')
    testsuite.set('name', 'Pester')
    testsuite.set('tests', str(total))
    testsuite.set('failures', str(failures))
    testsuite.set('skipped', str(skipped))
    testsuite.set('time', f"{time_total:.3f}")

    for classname, name, t, status, failure_message in testcases:
        tc_el = ET.SubElement(testsuite, 'testcase')
        tc_el.set('classname', classname)
        tc_el.set('name', name)
        tc_el.set('time', f"{t:.4f}")
        if status != 'Success':
            fail_el = ET.SubElement(tc_el, 'failure')
            fail_el.set('message', 'Test failed')
            fail_el.text = failure_message or None

    # Write output xml with declaration
    try:
        ET.register_namespace('', '')
        tree_out = ET.ElementTree(testsuites)
        tree_out.write(output_path, encoding='utf-8', xml_declaration=True)
        print(f"Wrote converted JUnit results to: {output_path}")
    except Exception as e:
        print(f"Failed to write output '{output_path}': {e}")
        return 0

    return 0


if __name__ == '__main__':
    if len(sys.argv) < 3:
        print("Usage: pester_nunit2_to_junit.py <input.xml> <output.xml>")
        sys.exit(0)
    inp = Path(sys.argv[1])
    outp = Path(sys.argv[2])
    sys.exit(convert(inp, outp))
