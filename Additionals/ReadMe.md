```
[out:json][timeout:25];

// fetch area to search in
(
  {{geocodeArea:Berlin}};
  {{geocodeArea:Brandenburg}};
)->.searchArea;

// gather results
(
  node[railway~"^(station|halt|stop)$"](area.searchArea);
  way[railway=rail][usage!~"^(industrial)$"](area.searchArea);
  relation[route~"^(rail|railway|tracks)$"](area.searchArea);
);

// print results
out center;
```
