
/**
 * Provides a simplified interface for interacting with a given
 * date object, where the getUTC and setUTC functions are combined into
 * the non-UTC versions. The UTC functions are called if local is false.
 *
 * If the date parameter is Date or DateWrapper, a copy of the date will
 * be constructed. For DateWrappers, value of local will also be copied
 * if local is not specified. An array can be used to pass multiple
 * parameters to the Date constructor.
 *
 * Using any other date parameter that doesn't evaluate to false will
 * construct a Date using the same parameter. However if it is
 * falsy, a new Date is constructed without parameters.
 *
 * @param {(Date|DateWrapper|object|number)} date
 * @param {boolean} local
 */
export default class DateWrapper {
    constructor(date, local) {
        if (!date) {
            this._date = new Date();
        } else if (date instanceof Date) {
            this._date = new Date(date);
        } else if (date instanceof DateWrapper) {
            this._date = new Date(date._date);
            if (local === undefined) { this.local = date.local }
        } else {
            if (Array.isArray(date)) {
                this._date = new Date(...date);
            } else {
                this._date = new Date(date);
            }
        }

        if (local !== undefined) {
            this.local = local;
        }
    }

    getDate() {
        return this.local ?
              this._date.getDate()
            : this._date.getUTCDate();
    }

    getDay() {
        return this.local ?
            this._date.getDay()
          : this._date.getUTCDay();
    }

    getFullYear() {
        return this.local ?
            this._date.getFullYear()
          : this._date.getUTCFullYear();
    }

    getHours() {
        return this.local ?
            this._date.getHours()
          : this._date.getUTCHours();
    }

    getMilliseconds() {
        return this.local ?
            this._date.getMilliseconds()
          : this._date.getUTCMilliseconds();
    }

    getMinutes() {
        return this.local ?
            this._date.getMinutes()
          : this._date.getUTCMinutes();
    }

    getMonth() {
        return this.local ?
            this._date.getMonth()
          : this._date.getUTCMonth();
    }

    getSeconds() {
        return this.local ?
            this._date.getSeconds()
          : this._date.getUTCSeconds();
    }

    getTime() {
        return this._date.getTime();
    }

    getTimezoneOffset() {
        return this._date.getTimezoneOffset();
    }

    /**
     * @deprecated
     */
    getYear() {
        return this._date.getYear();
    }

    setDate(day) {
        return this.local ?
            this._date.setDate(day)
          : this._date.setUTCDate(day);
    }

    setFullYear(year, month = undefined, date = undefined) {
        return this.local ?
            this._date.setFullYear(...filterUndefined([year, month, date]))
          : this._date.setUTCFullYear(...filterUndefined([year, month, date]));
    }

    setHours(hours, minutes = undefined, seconds = undefined, ms = undefined) {
        return this.local ?
            this._date.setHours(...filterUndefined([hours, minutes, seconds, ms]))
          : this._date.setUTCHours(...filterUndefined([hours, minutes, seconds, ms]));
    }

    setMilliseconds(ms) {
        return this.local ?
            this._date.setMilliseconds(ms)
          : this._date.setUTCMilliseconds(ms);
    }

    setMinutes(minutes, seconds = undefined, ms = undefined) {
        return this.local ?
            this._date.setMinutes(...filterUndefined([minutes, seconds, ms]))
          : this._date.setUTCMinutes(...filterUndefined([minutes, seconds, ms]));
    }

    setMonth(month, day = undefined) {
        return this.local ?
            this._date.setMonth(...filterUndefined([month, day]))
          : this._date.setUTCMonth(...filterUndefined([month, day]));
    }

    setSeconds(seconds, ms = undefined) {
        return this.local ?
            this._date.setSeconds(...filterUndefined([seconds, ms]))
          : this._date.setUTCSeconds(...filterUndefined([seconds, ms]));
    }

    setTime(time) {
        return this._date.setTime(time);
    }

    /**
     * @deprecated
     */
    setYear(year) {
        return this._date.setYear(year);
    }

    toDateString() {
        return this._date.toDateString();
    }

    toISOString() {
        return this._date.toISOString();
    }

    toJSON() {
        return this._date.toJSON();
    }

    /**
     * @deprecated
     */
    toGMTString() {
        return this._date.toGMTString();
    }

    toLocaleDateString(locales = undefined, options = undefined) {
        return this._date.toLocaleDateString(...filterUndefined([locales, options]));
    }

    toLocaleString(locales = undefined, options = undefined) {
        return this._date.toLocaleString(...filterUndefined([locales, options]));
    }

    toLocaleTimeString(locales = undefined, options = undefined) {
        return this._date.toLocaleTimeString(...filterUndefined([locales, options]));
    }

    toString() {
        return this._date.toString();
    }

    toTimeString() {
        return this._date.toTimeString();
    }

    toUTCString() {
        return this._date.toUTCString();
    }

    valueOf() {
        return this._date.valueOf();
    }
}

function filterUndefined(array) {
    return array.filter(val => val != undefined);
}
