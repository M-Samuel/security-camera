import sys
import json
import re


def parse_line(line):
    regex_pattern = r'\d+ (?:person|car)s*'
    matches = re.findall(regex_pattern, line)
    return matches

for line in sys.stdin:
    parsed_output = parse_line(line)
    if(parsed_output == []):
        continue;
    json_output = json.dumps(parsed_output)
    print(json_output)

