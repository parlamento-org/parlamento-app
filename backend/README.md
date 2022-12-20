# Microservice backend

The `AspNet` backend for this microservice.

## Database connection

To specify a `SQLite` database to use rather than the default one, fill the `DatabaseFilePath` section in `src/appsettings.local.json`.

## Project structure

- **src/**: Main source files of the application
- **test/**: Unit tests, used to test single components or functions in the application
- **integrations-tests/**: Integration tests, used to test the application running as a whole

## API Documentation

The API specification can be consulted on [SwaggerHub](https://app.swaggerhub.com/apis-docs/UP201906159_1/brand-config/1.0#/) or directly in the [yaml documentation](./docs/api.yaml).
