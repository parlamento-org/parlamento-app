cleanup() {
  echo "cleaning up..."
  # Our cleanup code goes here
}

trap 'kill "${child_pid}"; wait "${child_pid}"' SIGINT SIGTERM

docker-compose up dev-parlamento-fe $@ &

child_pid="$!"


ID=""
while [ -z "$ID" ]; do
    ID=$(docker ps -qf "name=dev-parlamento-fe")
done

docker attach $ID

wait "${child_pid}"
