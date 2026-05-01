-- Seed a demo board so the UI has something to show on first run.
INSERT INTO boards  (name, description) VALUES ('Product Backlog', 'Default demo board');

INSERT INTO columns (boardid, title, color, position) VALUES
    (1, 'Backlog',     '#6b7280', 0),
    (1, 'In Progress', '#3b82f6', 1),
    (1, 'Review',      '#f59e0b', 2),
    (1, 'Done',        '#10b981', 3);

INSERT INTO cards (boardid, columnid, title, description, position, priority, metadata) VALUES
    (1, 1, 'Set up CI pipeline',  'GitHub Actions with dotnet test + docker build', 0, 2, '{}'),
    (1, 1, 'Design auth flow',    'OAuth2 + JWT — decide on provider',              1, 1, '{}'),
    (1, 2, 'Implement board API', 'GET /boards/{id} with columns + cards',          0, 2, '{"labels":["backend"]}'),
    (1, 2, 'Blazor board shell',  'Dark industrial layout, column + card comps',    1, 1, '{"labels":["frontend"]}'),
    (1, 3, 'SignalR integration', 'Hub groups per board, presence updates',          0, 3, '{}'),
    (1, 4, 'Initial scaffold',   'Solution setup, three projects, CI green',         0, 1, '{}');
