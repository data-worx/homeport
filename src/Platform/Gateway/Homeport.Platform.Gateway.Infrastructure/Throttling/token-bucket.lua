-- SPDX-License-Identifier: AGPL-3.0-or-later

-- Takes one token from a tenant's bucket, atomically: Valkey runs a script
-- to the end before it runs any other command, so parallel requests through
-- any number of gateway replicas never take more tokens than the bucket holds.
--
-- The time is Valkey's own, so replicas whose clocks drift still share one
-- bucket. The key expires once the bucket would be full again; a missing
-- key is a full bucket.
--
-- KEYS[1]  the tenant's bucket
-- ARGV[1]  how many tokens the bucket holds (burst), at least 1
-- ARGV[2]  how many tokens it refills per second, at least 1
--
-- Returns { taken (1 or 0), whole tokens left,
--           milliseconds until a token is free, milliseconds until full }

local burst = tonumber(ARGV[1])
local rate = tonumber(ARGV[2])

local time = redis.call('TIME')
local now = tonumber(time[1]) * 1000000 + tonumber(time[2])

local bucket = redis.call('HMGET', KEYS[1], 'tokens', 'at')
local tokens = tonumber(bucket[1])
local at = tonumber(bucket[2])
if tokens == nil or at == nil then
  tokens = burst
  at = now
end

-- A plan that shrank caps the tokens left at its new size.
local elapsed = math.max(0, now - at)
tokens = math.min(burst, tokens + elapsed * rate / 1000000)

local taken = 0
if tokens >= 1 then
  tokens = tokens - 1
  taken = 1
end

local next_token = 0
if tokens < 1 then
  next_token = math.ceil((1 - tokens) * 1000 / rate)
end
local full = math.max(1, math.ceil((burst - tokens) * 1000 / rate))

-- Formatted by hand: tostring keeps 14 digits, too few for microseconds.
redis.call('HSET', KEYS[1], 'tokens', string.format('%.6f', tokens), 'at', string.format('%.0f', now))
redis.call('PEXPIRE', KEYS[1], full)

return { taken, math.floor(tokens), next_token, full }
