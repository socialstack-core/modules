import { ApiContent } from 'UI/Functions/WebRequest';
import { Role } from 'Api/Role';
import { Locale } from 'Api/Locale';
import { User } from 'Api/User';
// File generated from the C# definition

declare global {

    interface Session {

        /**
         * Set when the UI is currently waiting for the sessions user info to load.
         */
        loadingUser?: Promise<Session>

        /**
         * Optionally provided as the 'real' user when the current user is impersonating someone else.
         * Use sparingly as overuse would of course make impersonation relatively meaningless.
         */
        realUser?: User

		/**
         * The current session Role
         */
        role?: Role

		/**
         * The current session Locale
         */
        locale?: Locale

		/**
         * The current session User
         */
        user?: User

		/**
         * The current session User
         */
        realuser?: User

    }
    interface SessionResponse {

		/**
         * The pre-expanded Role
         */
        role?: ApiContent<Role>

		/**
         * The pre-expanded Locale
         */
        locale?: ApiContent<Locale>

		/**
         * The pre-expanded User
         */
        user?: ApiContent<User>

		/**
         * The pre-expanded User
         */
        realuser?: ApiContent<User>

	}
}

export { };