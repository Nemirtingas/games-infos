#pragma once

namespace boost {

    template <typename T>
    struct add_pointer
    {
        typedef typename std::remove_reference<T>::type no_ref_type;
        typedef no_ref_type* type;
    };

    template <class T> using add_pointer_t = typename add_pointer<T>::type;

} // namespace boost

namespace boost {
    namespace detail {

        template<typename Function> struct function_traits_helper;

        template<typename R>
        struct function_traits_helper<R(*)(void)>
        {
            static constexpr unsigned arity = 0;
            typedef R result_type;
        };

        template<typename R, typename T1>
        struct function_traits_helper<R(*)(T1)>
        {
            static constexpr unsigned arity = 1;
            typedef R result_type;
            typedef T1 arg1_type;
            typedef T1 argument_type;
        };

        template<typename R, typename T1, typename T2>
        struct function_traits_helper<R(*)(T1, T2)>
        {
            static constexpr unsigned arity = 2;
            typedef R result_type;
            typedef T1 arg1_type;
            typedef T2 arg2_type;
            typedef T1 first_argument_type;
            typedef T2 second_argument_type;
        };

        template<typename R, typename T1, typename T2, typename T3>
        struct function_traits_helper<R(*)(T1, T2, T3)>
        {
            static constexpr unsigned arity = 3;
            typedef R result_type;
            typedef T1 arg1_type;
            typedef T2 arg2_type;
            typedef T3 arg3_type;
        };

        template<typename R, typename T1, typename T2, typename T3, typename T4>
        struct function_traits_helper<R(*)(T1, T2, T3, T4)>
        {
            static constexpr unsigned arity = 4;
            typedef R result_type;
            typedef T1 arg1_type;
            typedef T2 arg2_type;
            typedef T3 arg3_type;
            typedef T4 arg4_type;
        };

        template<typename R, typename T1, typename T2, typename T3, typename T4,
            typename T5>
        struct function_traits_helper<R(*)(T1, T2, T3, T4, T5)>
        {
            static constexpr unsigned arity = 5;
            typedef R result_type;
            typedef T1 arg1_type;
            typedef T2 arg2_type;
            typedef T3 arg3_type;
            typedef T4 arg4_type;
            typedef T5 arg5_type;
        };

        template<typename R, typename T1, typename T2, typename T3, typename T4,
            typename T5, typename T6>
        struct function_traits_helper<R(*)(T1, T2, T3, T4, T5, T6)>
        {
            static constexpr unsigned arity = 6;
            typedef R result_type;
            typedef T1 arg1_type;
            typedef T2 arg2_type;
            typedef T3 arg3_type;
            typedef T4 arg4_type;
            typedef T5 arg5_type;
            typedef T6 arg6_type;
        };

        template<typename R, typename T1, typename T2, typename T3, typename T4,
            typename T5, typename T6, typename T7>
        struct function_traits_helper<R(*)(T1, T2, T3, T4, T5, T6, T7)>
        {
            static constexpr unsigned arity = 7;
            typedef R result_type;
            typedef T1 arg1_type;
            typedef T2 arg2_type;
            typedef T3 arg3_type;
            typedef T4 arg4_type;
            typedef T5 arg5_type;
            typedef T6 arg6_type;
            typedef T7 arg7_type;
        };

        template<typename R, typename T1, typename T2, typename T3, typename T4,
            typename T5, typename T6, typename T7, typename T8>
        struct function_traits_helper<R(*)(T1, T2, T3, T4, T5, T6, T7, T8)>
        {
            static constexpr unsigned arity = 8;
            typedef R result_type;
            typedef T1 arg1_type;
            typedef T2 arg2_type;
            typedef T3 arg3_type;
            typedef T4 arg4_type;
            typedef T5 arg5_type;
            typedef T6 arg6_type;
            typedef T7 arg7_type;
            typedef T8 arg8_type;
        };

        template<typename R, typename T1, typename T2, typename T3, typename T4,
            typename T5, typename T6, typename T7, typename T8, typename T9>
        struct function_traits_helper<R(*)(T1, T2, T3, T4, T5, T6, T7, T8, T9)>
        {
            static constexpr unsigned arity = 9;
            typedef R result_type;
            typedef T1 arg1_type;
            typedef T2 arg2_type;
            typedef T3 arg3_type;
            typedef T4 arg4_type;
            typedef T5 arg5_type;
            typedef T6 arg6_type;
            typedef T7 arg7_type;
            typedef T8 arg8_type;
            typedef T9 arg9_type;
        };

        template<typename R, typename T1, typename T2, typename T3, typename T4,
            typename T5, typename T6, typename T7, typename T8, typename T9,
            typename T10>
        struct function_traits_helper<R(*)(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>
        {
            static constexpr unsigned arity = 10;
            typedef R result_type;
            typedef T1 arg1_type;
            typedef T2 arg2_type;
            typedef T3 arg3_type;
            typedef T4 arg4_type;
            typedef T5 arg5_type;
            typedef T6 arg6_type;
            typedef T7 arg7_type;
            typedef T8 arg8_type;
            typedef T9 arg9_type;
            typedef T10 arg10_type;
        };

    } // end namespace detail

    template<typename Function>
    struct function_traits :
        public boost::detail::function_traits_helper<typename boost::add_pointer<Function>::type>
    {
    };

    template<typename ...args>
    struct function_traits<std::function<args...>> :
        public boost::detail::function_traits_helper<typename boost::add_pointer<args...>::type>
    {
    };

}