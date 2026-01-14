import * as dateTools from 'UI/Functions/DateTools';
import { useState, useEffect, useMemo } from 'react';

/**
 * Props for the Html component.
 */
interface TimeProps extends React.HTMLAttributes<HTMLTimeElement> {
	/**
	 * The date to display.
	 */
	date?: Dateish,

	/**
	 * Optional update rate, in seconds. If not specified 10 seconds is assumed.
	 */
	updateRate?: number,

	/**
	 * Display an absolute time rather than a time ago.
	 */
	absolute?: boolean,

	/**
	 * Display an absolute time with its date too.
	 */
	withDate?: boolean,

	/**
	 * Provides options for how the date is displayed.
	 * If absolute and with date are true, the date is visible on screen 
	 * otherwise it is visible as the title (mouse over on desktop).
	 */
	dateDisplay?: DateTextOptions,
}

/**
 * Displays a time string from the given date.
 * @param date
 * @returns
 */
function timeOnly(date : Date){
	var hours = date.getHours();
	var mins = date.getMinutes();
	var minsStr = (mins < 10) ? '0' + mins : mins.toString();
	var hoursStr = (hours < 10) ? '0' + hours : hours.toString();
	return hoursStr + ':' + minsStr;
}

/**
 * True if the given date is on this day.
 * @param date
 * @returns
 */
function isToday(date : Date) {
	return new Date(date).setHours(0, 0, 0, 0) == new Date().setHours(0, 0, 0, 0);
}

/**
 * True if the given date is on this week.
 * @param date
 * @returns
 */
function isThisWeek(date : Date) {
	var now = new Date();
	var start = dateTools.addDays(now, -7);
	return (date <= now && date >= start);
}

interface DateTextOptions {
	/**
	 * True if the date should be as compact as possible.
	 */
	compact?: boolean,
	/**
	 * A short form of the day.
	 */
	shortDay?: boolean,
	/**
	 * Displays only the day.
	 */
	compactDayOnly?: boolean
}

/**
 * Renders the given date as text.
 * @param date
 * @returns
 */
function dateText(date : Date, opts? : DateTextOptions) : string{
	var monthIndex = date.getMonth();
	var dayIndex = date.getDate();

	var dayStr;
	var { compact, shortDay, compactDayOnly } = opts || {};

	var fullYear = date.getFullYear();
	var nowYear = new Date().getFullYear();
	var yearText = "";

	if (nowYear != fullYear) {
		yearText = " " + fullYear;
	}

	if (compact) {
		if (isToday(date)) {
			return timeOnly(date);
		}

		if (isThisWeek(date)) {
			var dayIndex = date.getDay();

			dayStr = shortDay ? dateTools.shortDayNames[dayIndex] : dateTools.dayNames[dayIndex];

			if (!compactDayOnly) {
				dayStr += " " + timeOnly(date);
			}

			return dayStr;
		}

	}

	dayStr = dateTools.ordinal(dayIndex as int) + " " + dateTools.monthNames[monthIndex] + yearText;

	if (compact && compactDayOnly) {
		return dayStr;
	}

	return dayStr + " " + timeOnly(date);
}

/**
 * Constructs a 'x ago' style time string.
 * @param date
 * @param absolute
 * @param withDate
 * @param dateOpts
 * @returns
 */
function timeAgoString(date : Date, absolute?: boolean, withDate?: boolean, dateOpts?: DateTextOptions){
	if (!date) {
		return '';
	}

	if (absolute) {
		if (withDate) {
			return dateText(date, dateOpts);
		}

		return timeOnly(date);
	}

	var seconds = (Date.now() - date.getTime()) / 1000;

	if (seconds < 60) {
		return `Just now`;
	}

	var minutes = (seconds / 60);

	if (minutes < 60) {
		var flooredMinutes = Math.floor(minutes);
		return `${flooredMinutes} m ago`;
	}

	var hours = minutes / 60;

	if (hours < 24) {
		var flooredHours = Math.floor(hours);
		return `${flooredHours} h ago`;
	}

	var days = hours / 24;
	var flooredDays = Math.floor(days);
	return `${flooredDays} d ago`;
}

/**
* Displays "x ago" phrase, or just an absolute date/time. 'ago' is the default unless absolute is specified.
*/
const Time: React.FC<TimeProps> = ({ date, updateRate, absolute, withDate, dateDisplay, ...props }) => {
	const jsDate = useMemo(() => date ? dateTools.isoConvert(date) : new Date(), [date]);
	const [agoTime, setAgoTime] = useState('');

	useEffect(() => {
		setAgoTime(timeAgoString(jsDate, absolute, withDate, dateDisplay));
	}, [date, absolute, dateDisplay, jsDate, withDate]);

	useEffect(() => {
		if (updateRate && updateRate <= 0) {
			return;
		}

		var x = setInterval(() => setAgoTime(timeAgoString(jsDate, absolute, withDate, dateDisplay)), (updateRate || 10) * 1000);

		return () => {
			clearInterval(x);
		};
	}, [absolute, dateDisplay, jsDate, updateRate, withDate]);

	var isoString = '';
	var title = '';
	var isoString = jsDate.toISOString();
	var title = dateText(jsDate, dateDisplay);
	
	return <time title={title} dateTime={isoString} {...props}>
		{agoTime}
	</time>;
}

export default Time;
